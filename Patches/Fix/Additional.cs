using HarmonyLib;
using System;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class Additional
    {
        private static readonly Action<Raycaster, Unity_Overlay> UpdateWebViewHoverSession = AccessTools.MethodDelegate<Action<Raycaster, Unity_Overlay>>(AccessTools.Method(typeof(Raycaster), "UpdateWebViewHoverSession"));
        private static readonly Action<Raycaster, bool> EndWebViewHoverSession = AccessTools.MethodDelegate<Action<Raycaster, bool>>(AccessTools.Method(typeof(Raycaster), "EndWebViewHoverSession"));

        //# tmp xsoverlay update
        [HarmonyPatch(typeof(Raycaster), "OnPointerPress")]
        [HarmonyPrefix]
        public static bool FixTriggerDesktopToBecomeActive(Raycaster __instance, PointerPressEvent pointerPressEvent, MouseInputDevice ___InputDevice)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return true;
            if (pointerPressEvent.InputSource != ___InputDevice.InputSource) return true;

            if (!EventBridge.IsActiveHand(__instance))
            {
                // Become active hand and skip sending touch event to desktop
                if (EventBridge.IsOverlayDesktpOrWindowCapture(__instance.HoveringOverlay))
                {
                    XSOEventSystem.Current.EventTakeControlOfDesktopCursor(__instance);

                    if (!XConfig.TwoHandedMode.Value)
                        return false;
                }

                // Keyboard two handed
                if (EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay))
                {
                    EndWebViewHoverSession(__instance, false);
                    UpdateWebViewHoverSession(__instance, __instance.HoveringOverlay);
                    XSOEventSystem.Current.EventTakeControlOfDesktopCursor(__instance);
                }
            }

            return true;
        }
    }
}
