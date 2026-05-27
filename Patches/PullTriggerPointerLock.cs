using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class PullTriggerPointerLock
    {
        public class RaycasterState
        {
            public bool IsBlock = false;
            public bool IsStopping = false;
            public Vector2 DesktopCoordinates = new();
            public Coroutine Coroutine;
        }
        public static readonly ConditionalWeakTable<Raycaster, RaycasterState> InstanceState = new();

        private static readonly Func<Raycaster, float> GetTriggerAxis = AccessTools.MethodDelegate<Func<Raycaster, float>>(AccessTools.Method(typeof(Raycaster), "GetTriggerAxis"));

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void Init(Raycaster __instance)
        {
            if (!IsEnable() || !IsHand(__instance)) return;

            InstanceState.Add(__instance, new());
        }

        [HarmonyPatch(typeof(Raycaster), "Update")]
        [HarmonyPostfix]
        public static void ListenTriggerAxis(Raycaster __instance, bool ___HadMouseInputDown, bool ___HoldingTouch, bool ___DraggingTouch)
        {
            if (!IsEnable() || !IsHand(__instance)) return;

            if (EventBridge.IsActiveHand(__instance))
                if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                {
                    Unity_Overlay hover = __instance.HoveringOverlay;
                    if (hover != null && hover.IsDesktopOrWindowCapture)
                    {
                        if (GetTriggerAxis(__instance) > 0f && !___HadMouseInputDown && !___HoldingTouch && !___DraggingTouch)
                        {
                            if (Data.IsStopping)
                                Plugin.Instance.StopCoroutine(Data.Coroutine);

                            Data.IsStopping = false;
                            Data.IsBlock = true;
                        }
                        else if (Data.IsBlock && !Data.IsStopping)
                        {
                            Data.IsStopping = true;
                            Data.Coroutine = Plugin.Instance.StartCoroutine(UnblockDelay(__instance));
                        }
                    }
                }
        }

        [HarmonyPatch(typeof(Raycaster), "PointerHoverAndStateManagement")]
        [HarmonyPrefix]
        public static bool BlockCursorMovement(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsEnable() || !IsHand(__instance)) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                if (Data.IsBlock)
                    ___DesktopCoordinates = Data.DesktopCoordinates;
                else
                    Data.DesktopCoordinates = ___DesktopCoordinates;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SetVisualCursorTransform")]
        [HarmonyPrefix]
        public static bool BlockPointerMovement(Raycaster __instance)
        {
            if (!IsEnable() || !IsHand(__instance) || !PointerDoubleClickDelay.IsEnable()) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPrefix]
        public static bool BlockSearchForOverlays(Raycaster __instance)
        {
            if (!IsEnable() || !IsHand(__instance) || !PointerDoubleClickDelay.IsEnable()) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static bool InputClickLockPosition(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsEnable() || !IsHand(__instance)) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                if (Data.IsBlock)
                    ___DesktopCoordinates = Data.DesktopCoordinates;

            return true;
        }

        [HarmonyPatch(typeof(MouseInputDevice), nameof(MouseInputDevice.StartClickFreezePeriod))]
        [HarmonyPrefix]
        public static bool BlockOriginalDoubleClickDelay()
        {
            return !IsEnable();
        }

        private static IEnumerator UnblockDelay(Raycaster instance)
        {
            yield return new WaitForSecondsRealtime(XSettingsManager.Instance.Settings.DoubleClickDelay);

            if (InstanceState.TryGetValue(instance, out RaycasterState Data))
            {
                Data.IsBlock = false;
                Data.IsStopping = false;
            }
        }

        private static bool IsEnable()
        {
            return XConfig.PullTriggerPointerLock.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
