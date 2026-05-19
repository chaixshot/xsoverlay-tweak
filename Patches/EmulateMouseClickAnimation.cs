using HarmonyLib;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class EmulateMouseClickAnimation
    {
        [HarmonyPatch(nameof(Raycaster.HandleClicksForDesktopWindows))]
        [HarmonyPostfix]
        public static void HandleClicksForDesktopWindows(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay)
        {
            if (!IsEnable()) return;

            ___VisualCursorElementClickAnimationOverlay.gameObject.SetActive(value: true);
        }

        private static bool IsEnable()
        {
            return XConfig.EmulateMouseClickAnimation.Value;
        }
    }
}
