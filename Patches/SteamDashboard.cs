using HarmonyLib;
using System;
using System.Collections.Generic;

namespace xsoverlay_tweak.Patches
{
    internal class SteamDashboard
    {
        private static readonly Dictionary<string, Unity_Overlay> OverlayDictionary = [];

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            XConfig.DashboardNotification.SettingChanged += RefreshAll;
            XConfig.DashboardPointer.SettingChanged += RefreshAll;
            XConfig.DashboardSettings.SettingChanged += RefreshAll;
            XConfig.DashboardWindow.SettingChanged += RefreshAll;
            XConfig.DashboardWrist.SettingChanged += RefreshAll;
            XConfig.Dashboardkeyboard.SettingChanged += RefreshAll;
        }

        [HarmonyPatch(typeof(Unity_Overlay), "Start")]
        [HarmonyPostfix]
        public static void OverlayStart(Unity_Overlay __instance)
        {
            if (__instance == null) return;
            string overlayName = __instance.overlayName;
            if (string.IsNullOrEmpty(overlayName)) return;

            OverlayDictionary[overlayName] = __instance;
            UpdateDashboardStatus(__instance);
        }

        [HarmonyPatch(typeof(Unity_Overlay), "OnDestroy")]
        [HarmonyPrefix]
        public static bool OnDestroy(Unity_Overlay __instance)
        {
            if (__instance != null && !string.IsNullOrEmpty(__instance.overlayName))
                OverlayDictionary.Remove(__instance.overlayName);

            return true;
        }

        private static void RefreshAll(object sender, EventArgs e)
        {
            foreach (Unity_Overlay overlay in OverlayDictionary.Values)
                UpdateDashboardStatus(overlay);
        }

        private static void UpdateDashboardStatus(Unity_Overlay overlay)
        {
            string name = overlay.overlayName;
            bool state = false;

            if (name == "notification")
                state = XConfig.DashboardNotification.Value;
            else if (name == "settings")
                state = XConfig.DashboardSettings.Value;
            else if (name == "wrist")
                state = XConfig.DashboardWrist.Value;
            else if (name == "keyboard")
                state = XConfig.Dashboardkeyboard.Value;
            else if (name == "tooltip" || name == "splash")
                state = XConfig.DashboardSettings.Value || XConfig.DashboardWrist.Value;
            else if (name.Contains("Raycaster") || name.Contains("joystick"))
                state = XConfig.DashboardPointer.Value;
            else if (name.Contains("XSOverlay Window") || name == "window.settings" || name == "window.toolbar")
                state = XConfig.DashboardWindow.Value;

            overlay.isDashboardOverlay = state;

            if (state)
                overlay.isVisible = true;
        }
    }
}
