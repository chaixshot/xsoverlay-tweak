using HarmonyLib;
using System.Collections.Generic;
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
        private static float MinGrabInterval = 0.011f;

        private static int TargetFrameRate = -1;
        private static bool IsRefreshRateEnabled = false;

        public static List<string> RefreshRateList = ["'60 FPS'", "'75 FPS'", "'90 FPS'", "'120 FPS'", "'144 FPS'", "'200 FPS'", "'240 FPS'", "'300 FPS'", "'Unlimited'"];

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void InitializeEvents(DeviceManager __instance)
        {
            UpdateCache();

            // Listen to refresh rate change
            XConfig.RefreshRate.SettingChanged += (sender, args) =>
            {
                UpdateCache();
                if (IsRefreshRateEnable())
                    EventBridge.Ref_DeviceManager.GetHMDRefreshRate(__instance);
            };

            XConfig.OnlyHoverOverlay.SettingChanged += (sender, args) => UpdateCache();
            XConfig.OnlyInLayoutMod.SettingChanged += (sender, args) => UpdateCache();

            // Listen to edit mode change
            XSOEventSystem.OnToggleLayoutMode += (isEditMode) =>
            {
                if (IsRefreshRateEnable() && XConfig.OnlyInLayoutMod.Value)
                    if (!EfficiencyMode.IsEfficiencyModeEnable()) // Smooth overlay fadeout
                        EventBridge.Ref_DeviceManager.GetHMDRefreshRate(__instance);
            };
        }

        [HarmonyPatch(typeof(DeviceManager), "RegisterDevice")]
        [HarmonyPostfix]
        public static void WaitForHeadsetDetected(DeviceManager __instance, uint deviceId)
        {
            if (deviceId == __instance.PoseHandler.hmdIndex)
            {
                HMDRefreshRate = __instance.FetchHMDRefreshRate();
                MinGrabInterval = 1f / HMDRefreshRate;

                // Set default refresh rate to HMD refresh rate if it's not set by user
                if (XConfig.RefreshRate.Value == "Unknow")
                    XConfig.RefreshRate.Value = $"{HMDRefreshRate} FPS";

                // Add HMDRefreshRate to RefreshRateList if not exist
                if (!RefreshRateList.Exists(x => x.Equals($"'{HMDRefreshRate} FPS'")))
                    RefreshRateList.Add($"'{HMDRefreshRate} FPS'");

                UpdateCache();
            }
        }

        [HarmonyPatch(typeof(DeviceManager), "GetHMDRefreshRate")]
        [HarmonyPrefix]
        public static bool PatchHMDRefreshRate(DeviceManager __instance, ref Unity_SteamVR_Handler ___svr, ref bool ___HMDRefreshRateDetermined, ref int ___OldRefreshRate, ref int ___HMDRefreshRate)
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

                    if (IsRefreshRateEnable() && IsOnlyHoverOverlay() && IsOnlyInLayoutMode())
                        targetFrameRate = TargetFrameRate;

                    if (EfficiencyMode.ShouldInEfficiencyMode())
                        targetFrameRate = XConfig.InactiveRefreshRate.Value;

                    XSTools.ExecuteOnMainThread(delegate
                    {
                        if (Application.targetFrameRate != targetFrameRate)
                            Application.targetFrameRate = targetFrameRate;
                    });
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(Raycaster), "Grab")]
        [HarmonyPostfix]
        public static void FixPushPullSpeed(ref float ___GrabbedDistance)
        {
            if (!IsRefreshRateEnable()) return;

            float currentTime = Time.unscaledTime;
            if (currentTime - LastGrabTime < MinGrabInterval)
                ___GrabbedDistance = GrabbedDistance;
            else
            {
                GrabbedDistance = ___GrabbedDistance;
                LastGrabTime = currentTime;
            }
        }

        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool FixScrollingSpeed(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref int ___ScrollClicksPerSecond, ref float ____tickAccumulator, ref Vector2 ___CursorUVNormalized)
        {
            if (!IsRefreshRateEnable()) return true;

            // Pre-calculate combined constant to avoid redundant division and framerate sampling
            float scrollFactor = XSettingsManager.Instance.Settings.ScrollSpeed / HMDRefreshRate;

            float num = 0.2f;
            float y = ___InputDevice.NormalizedScrollAxis.y;
            float num2 = Mathf.Abs(y);
            if (num2 <= num || (float)___ScrollClicksPerSecond <= 0f)
            {
                return false;
            }

            if (__instance.HoveringOverlay.IsDesktopOrWindowCapture)
            {
                ____tickAccumulator += num2 * (float)___ScrollClicksPerSecond * scrollFactor;
                int num3 = (int)____tickAccumulator;
                if (num3 > 0)
                {
                    ____tickAccumulator -= num3;
                    MouseOperations.Scroll(((y > 0f) ? 1 : (-1)) * num3, XInputManager.sim);
                }
            }
            else if (__instance.HoveringOverlay.IsPluginApplication)
            {
                float num4 = y * scrollFactor;
                __instance.HoveringOverlay.WebViewHandler.WebView.Scroll(new Vector2(0f, 0f - num4), ___CursorUVNormalized);
            }

            return false;
        }

        private static void UpdateCache()
        {
            TargetFrameRate = GetFramrate(XConfig.RefreshRate.Value);
            IsRefreshRateEnabled = TargetFrameRate != (DeviceManager.Instance != null ? DeviceManager.Instance.HMDRefreshRate : HMDRefreshRate);
        }

        public static bool IsRefreshRateEnable()
        {
            return IsRefreshRateEnabled;
        }

        private static bool IsOnlyHoverOverlay()
        {
            return !XConfig.OnlyHoverOverlay.Value || EventBridge.IsHoverAnyOverlay;
        }

        private static bool IsOnlyInLayoutMode()
        {
            return !XConfig.OnlyInLayoutMod.Value || Overlay_Manager.Instance.editMode;
        }

        public static int GetFramrate(string speed)
        {
            if (speed.Equals("Unlimited"))
                return -1;
            else
                return int.Parse(speed.Replace(" FPS", ""));
        }

        public static string GetFramrate(int speed)
        {
            return RefreshRateList[speed].Replace("'", "");
        }
    }
}
