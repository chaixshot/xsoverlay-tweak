using HarmonyLib;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class InactivePointerOpacity
    {
        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void DetermineInactiveHandOpacity(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            if (!EventBridge.IsActiveHand(__instance))
                if (___VisualCursorElementOverlay.opacity.Equals(1) && !EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay))
                    ___VisualCursorElementOverlay.opacity = XConfig.InactivePointerOpacity.Value / 100f;
        }
    }
}
