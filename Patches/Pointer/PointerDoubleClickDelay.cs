using HarmonyLib;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class PointerDoubleClickDelay
    {
        [HarmonyPatch("SearchForOverlays")]
        [HarmonyPrefix]
        public static bool BlockPointerMovement(Raycaster __instance, ref MouseInputDevice ___InputDevice)
        {
            if (!IsEnable()) return true;
            if (EventBridge.IsOverlayWebView(__instance.HoveringOverlay)) return true;

            return !___InputDevice.ClickFreezeActive;
        }

        public static bool IsEnable()
        {
            return XConfig.PointerDoubleClickDelay.Value;
        }
    }
}