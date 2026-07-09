using HarmonyLib;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class OverlayGripAntiSlip
    {
        [HarmonyPatch(typeof(Raycaster), "CheckOverlayIntersection")]
        [HarmonyPostfix]
        public static void AntiSlip(Raycaster __instance, ref bool __result)
        {
            if (!IsEnable()) return;

            // If the controller is actively holding an overlay
            if (__instance?.HeldOverlay?.IsHeld == true)
            {
                // Force the raycaster to maintain a successful "hit" state 
                // so it doesn't slip off due to lag or fast hand movements
                __instance.HoveringOverlay = __instance.HeldOverlay;
                __result = true;
            }
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayGripAntiSlip.Value;
        }
    }
}