using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    internal class PullTriggerPointerLock
    {
        public class RaycasterState
        {
            public bool IsBlock = false;
            public bool IsStopping = false;
            public bool IsDown = false;
            public Vector2 DesktopCoordinates = new();
            public Coroutine Coroutine;
        }
        public static readonly ConditionalWeakTable<Raycaster, RaycasterState> InstanceState = new();

        private static readonly Func<Raycaster, float> GetTriggerAxis = AccessTools.MethodDelegate<Func<Raycaster, float>>(AccessTools.Method(typeof(Raycaster), "GetTriggerAxis"));

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void Init(Raycaster __instance)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance)) return;

            InstanceState.Add(__instance, new());
        }

        [HarmonyPatch(typeof(Raycaster), "Update")]
        [HarmonyPostfix]
        public static void ListenTriggerAxis(Raycaster __instance, bool ___HadMouseInputDown, bool ___HoldingTouch, bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance)) return;

            if (EventBridge.IsActiveHand(__instance))
                if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                {
                    if (__instance.HoveringOverlay != null)
                        if (!__instance.HoveringOverlay.IsHeld && !__instance.HoveringOverlay.IsLocked && __instance.HeldOverlay == null)
                            if (__instance?.HoveringOverlay?.IsDesktopOrWindowCapture == true || (XConfig.PullTriggerPointerLock.Value == 2 && __instance?.HoveringOverlay?.IsPluginApplication == true))
                            {
                                Data.IsDown = ___HadMouseInputDown || ___HoldingTouch || ___IsWebViewTouchEventDown;

                                if (GetTriggerAxis(__instance) > 0f && !Data.IsDown)
                                {
                                    if (Data.IsStopping)
                                        Plugin.Instance.StopCoroutine(Data.Coroutine);

                                    if (!Data.IsBlock)
                                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                                    Data.IsStopping = false;
                                    Data.IsBlock = true;

                                    return;
                                }
                            }

                    if (Data.IsBlock && !Data.IsStopping)
                    {
                        if (!Data.IsDown)
                            AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                        Data.IsStopping = true;
                        Data.IsDown = false;
                        Data.Coroutine = Plugin.Instance.StartCoroutine(UnblockDelay(__instance));
                    }
                }
        }

        [HarmonyPatch(typeof(Raycaster), "PointerHoverAndStateManagement")]
        [HarmonyPrefix]
        public static void BlockCursorMovement(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance)) return;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                if (Data.IsBlock)
                    ___DesktopCoordinates = Data.DesktopCoordinates;
                else
                    Data.DesktopCoordinates = ___DesktopCoordinates;
        }

        [HarmonyPatch(typeof(Raycaster), "SetVisualCursorTransform")]
        [HarmonyPrefix]
        public static bool BlockPointerMovement(Raycaster __instance)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance) || !PointerDoubleClickDelay.IsEnable()) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPrefix]
        public static bool BlockSearchForOverlays(Raycaster __instance)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance) || !PointerDoubleClickDelay.IsEnable()) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static void InputClickLockPosition(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance)) return;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                if (Data.IsBlock)
                    ___DesktopCoordinates = Data.DesktopCoordinates;
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
            return XConfig.PullTriggerPointerLock.Value != 0;
        }
    }
}