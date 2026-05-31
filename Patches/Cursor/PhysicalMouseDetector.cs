using HarmonyLib;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class PhysicalMouseDetector
    {
        public static bool IsPhysicalMovement = false;
        public static readonly MouseInputDetector mouseDetector = new();

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            mouseDetector.PhysicalMouseMoved += (x, y) =>
            {
                if (IsEnable())
                    IsPhysicalMovement = true;
            };
        }

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool ClickToRegainControl(Raycaster __instance)
        {
            if (IsPhysicalMovement)
            {
                IsPhysicalMovement = false;
                EventBridge.Ref_Raycaster.TakeControlOverCursorIfNotInControl(__instance);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SyncedOverlayUpdate")]
        [HarmonyPrefix]
        public static bool BlockSeningNewCursorPostion()
        {
            return !IsPhysicalMovement;
        }

        private static bool IsEnable()
        {
            return XConfig.PhysicalMouseDetector.Value;
        }
    }
}
