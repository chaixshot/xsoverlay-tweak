using HarmonyLib;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class AdditionalFix
    {
        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.MoveToEdgeOfWindowAndInheritRotation))]
        [HarmonyPrefix]
        public static bool MoveToEdgeOfWindowAndInheritRotation(Unity_Overlay TargetOverlay)
        {
            if (TargetOverlay == null || TargetOverlay.overlayTexture == null)
                return false;

            return true;
        }
    }
}
