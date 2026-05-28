using HarmonyLib;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class PinBlockInputNonEditMode
    {
        [HarmonyPatch("ComputeOverlayIntersections")]
        [HarmonyPostfix]
        public static void BlockInputNonEditMode(Raycaster __instance, ref bool __result)
        {
            if (!IsEnable()) return;

            if (__result)
            {
                if (!Overlay_Manager.Instance.editMode)
                    if (__instance?.HoveringOverlay?.isPinned == true && __instance?.HoveringOverlay?.IsLocked == true)
                    {
                        __instance.HoveringOverlay = null;
                        __result = false;
                    }
            }
        }

        private static bool IsEnable()
        {
            return XConfig.PinBlockInputNonEditMode.Value;
        }
    }
}
