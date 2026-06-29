using HarmonyLib;
using System;
using UnityEngine;
using Vuplex.WebView;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerWebView
    {
        public static readonly Action<Raycaster> HandleScrolling = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "HandleScrolling"));


        // Add additional check for Pointer hover WebView event of inactive hand
        [HarmonyPatch("OnCursorPluginApplication")]
        [HarmonyPrefix]
        public static bool ApplyInactiveFeatureHandToWebView(Raycaster __instance, bool canCursorInteract, Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;
            if (!EventBridge.IsRaycasterHand(__instance)) return true;

            canCursorInteract = canCursorInteract && EventBridge.IsActiveHand(__instance);

            if (__instance.HoveringOverlay.IsPluginApplication && __instance.HoveringOverlay.WebViewHandler != null && canCursorInteract)
            {
                if (DesktopCursorManager.Instance.GetCurrentInputDevice() == __instance)
                    (__instance.HoveringOverlay.WebViewHandler.WebView as IWithMovablePointer).MovePointer(___CursorUVNormalized);
                HandleScrolling(__instance);
            }

            return false;
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
