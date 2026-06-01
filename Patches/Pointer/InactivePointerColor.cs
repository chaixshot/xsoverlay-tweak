using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class InactivePointerColor
    {
        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void SetActiveColor(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (EventBridge.IsActiveHand(__instance) || EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay))
                ___VisualCursorElementOverlay.colorTint = XSettingsManager.Instance.Settings.AccentColor;
            else if (PhysicalMouseDetector.IsPhysicalMovement)
                ___VisualCursorElementOverlay.colorTint = Color.gray;
            else if (__instance?.HoveringOverlay?.IsLocked == false)
                ___VisualCursorElementOverlay.colorTint = Color.red;
        }

        public static bool IsEnable()
        {
            return XConfig.InactivePointerColor.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
