using HarmonyLib;
using System;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerColor
    {
        private static readonly Action<Raycaster> TakeControlOverCursorIfNotInControlDelegate = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));

        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void SetActiveColor(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (!EventBridge.IsActiveHand(__instance))
                ___VisualCursorElementOverlay.colorTint = Color.red;
            else if (__instance.HoveringOverlay != null && !__instance.HoveringOverlay.IsLocked)
                ___VisualCursorElementOverlay.colorTint = XSettingsManager.Instance.Settings.AccentColor;
        }

        [HarmonyPatch("DetermineCursorVisibility")]
        [HarmonyPostfix]
        public static void DetermineInactiveHandOpacity(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (!EventBridge.IsActiveHand(__instance))
                if (___VisualCursorElementOverlay.opacity.Equals(1))
                    ___VisualCursorElementOverlay.opacity = XConfig.ActivePointerOpacity.Value / 100f;
        }

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static bool HandleClickOnCaptureOverlayToBecomeActiveHandAndClick(Raycaster __instance)
        {
            if (XConfig.PointerActiveClick.Value)
                if (!EventBridge.IsActiveHand(__instance))
                {
                    TakeControlOverCursorIfNotInControlDelegate(__instance);

                    RayCastResult? desktopCoordinate = EventBridge.GetDesktopCoordinateDelegate(__instance);
                    MouseOperations.SetCursorPosition((int)desktopCoordinate.Value.desktopCoord.x, (int)desktopCoordinate.Value.desktopCoord.y);

                    __instance.CanClickDesktopCursor = true;
                }

            return true;
        }

        public static bool IsEnable()
        {
            return XConfig.ActivePointerColor.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
