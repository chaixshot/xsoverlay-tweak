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

        public static List<string> RefreshRateList = ["'60 FPS'", "'75 FPS'", "'90 FPS'", "'120 FPS'", "'144 FPS'", "'200 FPS'", "'240 FPS'", "'300 FPS'", "'Unlimited'"];

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void InitializeEvents(DeviceManager __instance)
        {
            // Listen to refresh rate change
            XConfig.RefreshRate.SettingChanged += (sender, args) =>
            {
                if (IsRefreshRateEnable())
                    EventBridge.Ref_DeviceManager.GetHMDRefreshRate(__instance);
            };

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

                // Set default refresh rate to HMD refresh rate if it's not set by user
                if (XConfig.RefreshRate.Value == "Unknow")
                    XConfig.RefreshRate.Value = $"{HMDRefreshRate} FPS";

                // Add HMDRefreshRate to RefreshRateList if not exist
                if (!RefreshRateList.Exists(x => x.Equals($"'{HMDRefreshRate} FPS'")))
                    RefreshRateList.Add($"'{HMDRefreshRate} FPS'");
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
                        targetFrameRate = GetFramrate(XConfig.RefreshRate.Value);

                    if (EfficiencyMode.ShouldInEfficiencyMode())
                        targetFrameRate = XConfig.InactiveRefreshRate.Value;

                    XSTools.ExecuteOnMainThread(delegate
                    {
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
            if (currentTime - LastGrabTime < (1f / HMDRefreshRate))
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

        public static bool IsRefreshRateEnable()
        {
            return GetFramrate(XConfig.RefreshRate.Value) != DeviceManager.Instance.HMDRefreshRate;
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
