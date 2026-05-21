using HarmonyLib;
using System;
using UnityEngine;
using uWindowCapture;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class ActivePointerColor
    {
        private static readonly Action<Raycaster> TakeControlOverCursorIfNotInControlDelegate = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));
        private static readonly Func<Raycaster, RayCastResult?> GetDesktopCoordinateDelegate = AccessTools.MethodDelegate<Func<Raycaster, RayCastResult?>>(AccessTools.Method(typeof(Raycaster), "GetDesktopCoordinate"));

        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void SetActiveColor(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (!IsActiveHand(__instance))
                ___VisualCursorElementOverlay.colorTint = Color.red;
            else if (!__instance.HoveringOverlay.IsLocked)
                ___VisualCursorElementOverlay.colorTint = XSettingsManager.Instance.Settings.AccentColor;
        }

        [HarmonyPatch("DetermineCursorVisibility")]
        [HarmonyPostfix]
        public static void DetermineCursorVisibility(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (!IsActiveHand(__instance))
                if (___VisualCursorElementOverlay.opacity.Equals(1))
                    ___VisualCursorElementOverlay.opacity = XConfig.ActivePointerOpacity.Value / 100f;
        }

        // XConfig.PointerActiveClick
        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("HandleClicksForDesktopWindows")]
        [HarmonyPrefix]
        public static bool HandleClicksForDesktopWindows(Raycaster __instance)
        {
            if (XConfig.PointerActiveClick.Value)
                if (!IsActiveHand(__instance))
                {
                    TakeControlOverCursorIfNotInControlDelegate(__instance);

                    RayCastResult? desktopCoordinate = GetDesktopCoordinateDelegate(__instance);
                    MouseOperations.SetCursorPosition((int)desktopCoordinate.Value.desktopCoord.x, (int)desktopCoordinate.Value.desktopCoord.y);

                    __instance.CanClickDesktopCursor = true;
                }

            return true;
        }

        private static bool IsActiveHand(Raycaster __instance)
        {
            if (PhysicalMouseDetector.IsPhysicalMovement)
                return false;
            else if (DesktopCursorManager.Instance.GetCurrentInputDevice() != __instance)
                return false;

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.ActivePointerColor.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
