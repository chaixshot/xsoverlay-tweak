using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class RefreshRate
    {
        private static int HMDRefreshRate = 90;
        private static float LastGrabTime;
        private static float GrabbedDistance = 0f;
        private static bool IsHoverAnyOverlay = false;
        private static CancellationTokenSource NotificationCancelToken;
        private static bool IsNotificationVisible = false;
        private static bool IsInEfficiencyMode = false;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void Start(DeviceManager __instance)
        {
            // Listen to Refresh Rate enable change
            XConfig.EnableRefreshRate.SettingChanged += (sender, args) =>
            {
                GetHMDRefreshRate();
            };

            // Listen to refresh rate change
            XConfig.RefreshRate.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    GetHMDRefreshRate();
            };

            // Listen to edit mode change
            XSOEventSystem.OnToggleLayoutMode += (isEditMode) =>
            {
                if (IsEfficiencyModeEnable())
                {
                    if (isEditMode) // Smooth overlay fadeout
                        GetHMDRefreshRate();
                }
                else if (IsEnable())
                    if (XConfig.OnlyInEditMod.Value)
                        GetHMDRefreshRate();
            };

            // Listen to notification push
            XSOEventSystem.OnQueueNotification += (notify) =>
            {
                if (IsEfficiencyModeEnable())
                {
                    IsNotificationVisible = true;
                    GetHMDRefreshRate();

                    // Cancel any previous notification timer
                    NotificationCancelToken?.Cancel();
                    NotificationCancelToken = new CancellationTokenSource();
                    CancellationToken token = NotificationCancelToken.Token;

                    Task.Delay(TimeSpan.FromSeconds(notify.timeout), token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                            IsNotificationVisible = false;
                    });
                }
            };

            // Listen to hovering overlay change
            {
                XSOEventSystem.OnSwitchHoveringOverlay += (raycaster, overlay) =>
                {
                    IsHoverAnyOverlay = true;

                    if (IsEnable())
                        if (XConfig.OnlyHoverOverlay.Value)
                            AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);

                    if (IsEfficiencyModeEnable())
                        AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);
                };

                XSOEventSystem.OnReleaseControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = false;

                    if (IsEnable())
                        if (XConfig.OnlyHoverOverlay.Value)
                            AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);

                    if (IsEfficiencyModeEnable())
                        AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);
                };
            }
        }

        [HarmonyPatch(typeof(DeviceManager), "GetHMDRefreshRate")]
        [HarmonyPrefix]
        public static bool GetHMDRefreshRate(DeviceManager __instance, ref Unity_SteamVR_Handler ___svr, ref bool ___HMDRefreshRateDetermined, ref int ___OldRefreshRate, ref int ___HMDRefreshRate)
        {
            if (___svr.isSteamVRConnected)
            {
                // Original
                {
                    ___HMDRefreshRate = __instance.FetchHMDRefreshRate();
                    HMDRefreshRate = ___HMDRefreshRate;

                    if (!___HMDRefreshRateDetermined)
                    {
                        XSTools.log("Detected Headset Refresh Rate: " + ___HMDRefreshRate);
                        ___HMDRefreshRateDetermined = true;
                    }

                    if (HMDRefreshRate != ___OldRefreshRate)
                    {
                        ___HMDRefreshRateDetermined = false;
                    }

                    ___OldRefreshRate = ___HMDRefreshRate;
                }

                // Modify
                {
                    int targetFrameRate = HMDRefreshRate;

                    if (ShouldInEfficiencyMode())
                    {
                        IsInEfficiencyMode = true;
                        Application.runInBackground = false;
                        EfficiencyModeController.SetEfficiencyMode(true);

                        targetFrameRate = 15;
                    }
                    else
                    {
                        if (IsInEfficiencyMode)
                        {
                            IsInEfficiencyMode = false;
                            Application.runInBackground = true;
                            EfficiencyModeController.SetEfficiencyMode(false);
                        }

                        if (IsEnable())
                            if (IsOnlyHoverOverlay())
                                if (IsOnlyInEditMode())
                                    targetFrameRate = XConfig.RefreshRate.Value.Equals(500) ? -1 : XConfig.RefreshRate.Value;
                    }

                    Plugin.Logger.LogError(targetFrameRate);

                    XSTools.ExecuteOnMainThread(delegate
                    {
                        Application.targetFrameRate = targetFrameRate;
                    });
                }
            }

            return false;
        }

        // Fix Push/Pull speed
        [HarmonyPatch(typeof(Raycaster), "Grab")]
        [HarmonyPostfix]
        public static void Grab(ref float ___GrabbedDistance)
        {
            if (!IsEnable()) return;

            float currentTime = Time.unscaledTime;
            if (currentTime - LastGrabTime < (1f / HMDRefreshRate))
                ___GrabbedDistance = GrabbedDistance;
            else
            {
                GrabbedDistance = ___GrabbedDistance;
                LastGrabTime = currentTime;
            }
        }

        // Fix Scrolling speed
        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool HandleScrolling(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref int ___ScrollClicksPerSecond, ref float ____tickAccumulator, ref Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;

            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            float scrollSpeedMultiplier = XSettingsManager.Instance.Settings.ScrollSpeed * (currentFPS / HMDRefreshRate);

            float num = 0.2f;
            float y = ___InputDevice.NormalizedScrollAxis.y;
            float num2 = Mathf.Abs(y);
            if (num2 <= num || (float)___ScrollClicksPerSecond <= 0f)
            {
                return false;
            }

            if (__instance.HoveringOverlay.IsDesktopOrWindowCapture)
            {
                ____tickAccumulator += num2 * (float)___ScrollClicksPerSecond * scrollSpeedMultiplier * Time.unscaledDeltaTime;
                int num3 = (int)____tickAccumulator;
                if (num3 > 0)
                {
                    ____tickAccumulator -= num3;
                    MouseOperations.Scroll(((y > 0f) ? 1 : (-1)) * num3, XInputManager.sim);
                }
            }
            else if (__instance.HoveringOverlay.IsPluginApplication)
            {
                float num4 = y * scrollSpeedMultiplier * Time.unscaledDeltaTime;
                __instance.HoveringOverlay.WebViewHandler.WebView.Scroll(new Vector2(0f, 0f - num4), ___CursorUVNormalized);
            }

            return false;
        }

        private static void GetHMDRefreshRate()
        {
            AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(DeviceManager.Instance, null);
        }

        private static bool IsEnable()
        {
            return XConfig.EnableRefreshRate.Value;
        }

        private static bool IsEfficiencyModeEnable()
        {
            return XConfig.EfficiencyMode.Value;
        }

        private static bool IsOnlyHoverOverlay()
        {
            return !XConfig.OnlyHoverOverlay.Value || IsHoverAnyOverlay;
        }

        private static bool IsOnlyInEditMode()
        {
            return !XConfig.OnlyInEditMod.Value || Overlay_Manager.Instance.editMode;
        }

        private static bool ShouldInEfficiencyMode()
        {
            Plugin.Logger.LogError($"{IsEfficiencyModeEnable()}, {!Overlay_Manager.Instance.editMode}, {!IsHoverAnyOverlay}, {!IsNotificationVisible}");
            return IsEfficiencyModeEnable() && !Overlay_Manager.Instance.editMode && !IsHoverAnyOverlay && !IsNotificationVisible;
        }
    }
}
