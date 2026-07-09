using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class TwoHandedMode
    {
        private class RaycasterData
        {
            public Vector3 LastRayHitPoint;
        }
        private static readonly ConditionalWeakTable<Raycaster, RaycasterData> RaycasterDictionary = new();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void FixHoveringReleaseControlEvent(Raycaster __instance)
        {
            XSOEventSystem.OnReleaseControlOfDesktopCursor += (raycaster) =>
            {
                if (!IsEnable()) return;
                if (raycaster == __instance) return;
                if (!EventBridge.IsRaycasterHand(__instance)) return;

                if (__instance.HoveringOverlay != null)
                    XSOEventSystem.Current.EventSwitchHoveringOverlay(__instance, __instance.HoveringOverlay);
            };
        }

        [HarmonyPatch("HandleClicksForDesktopWindows"), HarmonyPatch("HandleTouchInputForDesktopWindows"), HarmonyPatch("HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static void ClickToBecomeActiveHandAndDoClick(Raycaster __instance)
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

        [HarmonyPatch("OnDesktopCursor")]
        [HarmonyPrefix]
        public static void MoveToTakeControl(Raycaster __instance, Vector3 ___RayHitPoint)
        {
            if (!IsEnable()) return;

            if (!EventBridge.IsActiveHand(__instance, true))
            {
                RaycasterData data = RaycasterDictionary.GetOrCreateValue(__instance);

                if (Vector3.Distance(___RayHitPoint, data.LastRayHitPoint) > 0.01f)
                {
                    data.LastRayHitPoint = ___RayHitPoint;
                    XSOEventSystem.Current.EventTakeControlOfDesktopCursor(__instance);
                }
            }
        }

        [HarmonyPatch("HandleScrolling")]
        [HarmonyPrefix]
        public static bool ScrollingNonCurrentHandFix(Raycaster __instance)
        {
            if (!IsEnable()) return true;
            if (HandleScrolling.IsEnable()) return true;

            if (!EventBridge.IsActiveHand(__instance, true))
                return false;

            return true;
        }

        public static bool IsEnable()
        {
            return XConfig.TwoHandedMode.Value;
        }
    }
}