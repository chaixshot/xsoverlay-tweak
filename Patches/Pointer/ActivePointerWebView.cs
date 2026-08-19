using HarmonyLib;
using System;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerWebView
    {
        private static readonly Func<Raycaster, Unity_Overlay, bool> CanInteractWithWebView = AccessTools.MethodDelegate<Func<Raycaster, Unity_Overlay, bool>>(AccessTools.Method(typeof(Raycaster), "CanInteractWithWebView"));
        public static readonly Action<Raycaster> ClearHoverState = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "ClearHoverState"));

        // Listen for Pointer click WebView to become active hand
        [HarmonyPatch("BeginWebViewTouch")]
        [HarmonyPrefix]
        public static bool HandlePressOnWebViewTriggerToBecomeActive(Raycaster __instance)
        {
            if (!IsEnable()) return true;
            if (!EventBridge.IsRaycasterHand(__instance)) return true;
            if (EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay)) return true;

            // Become active hand and skip sending touch event to webview
            if (!EventBridge.IsActiveHand(__instance) && EventBridge.IsOverlayWebView(__instance.HoveringOverlay))
            {
                ClearHoverState(__instance);
                EventBridge.Ref_Raycaster.TakeControlOverCursorIfNotInControl(__instance);

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
