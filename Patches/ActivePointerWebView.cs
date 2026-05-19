using HarmonyLib;
using System;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerWebView
    {
        private static readonly Action<Raycaster> TakeControlOverCursorIfNotInControlDelegate = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));

        // Add additional check for Pointer hover WebView event of inactive hand
        [HarmonyPatch("OnCursorPluginApplication")]
        [HarmonyPrefix]
        public static bool OnCursorPluginApplication(Raycaster __instance, ref bool canCursorInteract, ref bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable()) return true;
            if (!IsHand(__instance)) return true;

            bool IsActiveHand = DesktopCursorManager.Instance.GetCurrentInputDevice() == __instance;

            canCursorInteract = canCursorInteract && IsActiveHand;

            return true;
        }

        // Listen for Pointer click WebView to become active hand
        [HarmonyPatch("HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool HandleTouchInputForWebApplications(Raycaster __instance)
        {
            if (!IsEnable()) return true;
            if (!IsHand(__instance)) return true;

            bool IsActive = DesktopCursorManager.Instance.GetCurrentInputDevice() == __instance;

            // Become active hand and skip sending touch event to webview
            if (!IsActive)
            {
                TakeControlOverCursorIfNotInControlDelegate(__instance);

                return false;
            }

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.ActivePointerWebView.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
