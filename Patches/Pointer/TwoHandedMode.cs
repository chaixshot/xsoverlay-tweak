using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
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

        [HarmonyPatch(typeof(Raycaster), "OnDesktopCursor")]
        [HarmonyPrefix]
        public static void MoveToTakeControl(Raycaster __instance, Vector3 ___RayHitPoint)
        {
            if (!IsEnable()) return;

            if (DesktopCursorManager.Instance.GetCurrentInputDevice() != __instance)
            {
                RaycasterData data = RaycasterDictionary.GetOrCreateValue(__instance);

                if (Vector3.Distance(___RayHitPoint, data.LastRayHitPoint) > 0.01f)
                {
                    data.LastRayHitPoint = ___RayHitPoint;
                    XSOEventSystem.Current.EventTakeControlOfDesktopCursor(__instance);
                }
            }
        }

        public static bool IsEnable()
        {
            return XConfig.TwoHandedMode.Value;
        }
    }
}