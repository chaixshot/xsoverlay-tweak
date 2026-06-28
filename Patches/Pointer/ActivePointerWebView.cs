using HarmonyLib;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerWebView
    {
        // Add additional check for Pointer hover WebView event of inactive hand
        [HarmonyPatch("OnCursorPluginApplication")]
        [HarmonyPrefix]
        public static void ApplyInactiveFeatureHandToWebView(Raycaster __instance, ref bool canCursorInteract, ref bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            canCursorInteract = canCursorInteract && EventBridge.IsActiveHand(__instance);
        }

        // Listen for Pointer click WebView to become active hand
        [HarmonyPatch("HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool HandlePressOnWebViewTriggerToBecomeActive(Raycaster __instance)
        {
            if (!IsEnable()) return true;
            if (!EventBridge.IsRaycasterHand(__instance)) return true;

            // Become active hand and skip sending touch event to webview
            if (!EventBridge.IsActiveHand(__instance) && EventBridge.IsOverlayWebView(__instance.HoveringOverlay))
            {
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
