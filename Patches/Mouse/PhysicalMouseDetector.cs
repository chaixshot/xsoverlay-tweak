using HarmonyLib;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Mouse
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

            XSOEventSystem.OnToggleLayoutMode += (isEditMode) =>
            {
                if (isEditMode && IsPhysicalMovement)
                    IsPhysicalMovement = false;
            };

            XConfig.PhysicalMouseDetector.SettingChanged += (sender, args) =>
            {
                if (!IsEnable())
                    IsPhysicalMovement = false;
            };
        }

        [HarmonyPatch(typeof(Raycaster), "OnPointerPress")]
        [HarmonyPrefix]
        public static bool ClickToRegainControl(Raycaster __instance, MouseInputDevice ___InputDevice, PointerPressEvent pointerPressEvent)
        {
            if (pointerPressEvent.InputSource != ___InputDevice.InputSource) return true;

            if (IsPhysicalMovement)
            {
                IsPhysicalMovement = false;
                XSOEventSystem.Current.EventTakeControlOfDesktopCursor(__instance);

                if (EventBridge.IsOverlayDesktpOrWindowCapture(__instance.HoveringOverlay))
                    return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "PointerHoverAndStateManagement")]
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
