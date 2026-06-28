using HarmonyLib;
using uWindowCapture;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class TwoHandedMode
    {

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static void ForDesktopWindows(Raycaster __instance)
        {
            if (!IsEnable()) return;

            if (!EventBridge.IsActiveHand(__instance, true))
            {
                EventBridge.Ref_Raycaster.TakeControlOverCursorIfNotInControl(__instance);

                RayCastResult? desktopCoordinate = EventBridge.Ref_Raycaster.GetDesktopCoordinate(__instance);
                MouseOperations.SetCursorPosition((int)desktopCoordinate.Value.desktopCoord.x, (int)desktopCoordinate.Value.desktopCoord.y);

                __instance.CanClickDesktopCursor = true;
            }
        }

        public static bool IsEnable()
        {
            return XConfig.TwoHandedMode.Value;
        }
    }
}
