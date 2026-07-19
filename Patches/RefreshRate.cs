using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Mouse;
using xsoverlay_tweak.Patches.Optimization;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class RefreshRate
    {
        public static int HMDRefreshRate = 90;
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
            return !XConfig.OnlyHoverOverlay.Value || (EventBridge.IsHoverAnyOverlay() && PhysicalMouseDetector.IsPhysicalMovement);
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
