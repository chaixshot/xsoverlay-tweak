using HarmonyLib;
using System;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerWebView
    {
        private static readonly Action<Raycaster, Unity_Overlay> UpdateWebViewHoverSession = AccessTools.MethodDelegate<Action<Raycaster, Unity_Overlay>>(AccessTools.Method(typeof(Raycaster), "UpdateWebViewHoverSession"));
        private static readonly Action<Raycaster, bool> EndWebViewHoverSession = AccessTools.MethodDelegate<Action<Raycaster, bool>>(AccessTools.Method(typeof(Raycaster), "EndWebViewHoverSession"));


        // Listen for Pointer click WebView to become active hand
        [HarmonyPatch("OnPointerPress")]
        [HarmonyPrefix]
        public static bool HandlePressOnWebViewTriggerToBecomeActive(Raycaster __instance, PointerPressEvent pointerPressEvent, MouseInputDevice ___InputDevice)
        {
            if (!IsEnable()) return true;
            if (!EventBridge.IsRaycasterHand(__instance)) return true;
            if (pointerPressEvent.InputSource != ___InputDevice.InputSource) return true;

            // Become active hand and skip sending touch event to webview
            if (!EventBridge.IsActiveHand(__instance) && EventBridge.IsOverlayWebView(__instance.HoveringOverlay, "wrist, notification, keyboard"))
            {
                EndWebViewHoverSession(__instance, true);
                UpdateWebViewHoverSession(__instance, __instance.HoveringOverlay);

                if (!XConfig.TwoHandedMode.Value)
                    return false;
            }

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.ActivePointerWebView.Value;
        }
    }
}
