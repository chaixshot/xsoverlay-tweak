using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;
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
            public bool WasSmooth = false;
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

            XConfig.PullTriggerPointerLock.SettingChanged += (sender, args) =>
            {
                if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                {
                    Data.IsStopping = true;
                    Data.Coroutine = Plugin.Instance.StartCoroutine(UnblockDelay(__instance));
                }
            };
        }

        [HarmonyPatch(typeof(Raycaster), "Update")]
        [HarmonyPostfix]
        public static void ListenTriggerAxis(Raycaster __instance, bool ___HadMouseInputDown, bool ___HoldingTouch, bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable() || !EventBridge.IsRaycasterHand(__instance)) return;
            if (!EventBridge.IsActiveHand(__instance)) return;
            if (!InstanceState.TryGetValue(__instance, out RaycasterState Data)) return;

            Unity_Overlay hovering = __instance.HoveringOverlay;
            if (hovering == null || hovering.IsHeld || hovering.IsLocked || __instance.HeldOverlay != null) return;

            bool isDesktopOrCapture = hovering.IsDesktopOrWindowCapture;
            bool isWebViewLock = XConfig.PullTriggerPointerLock.Value == 2 && hovering.IsPluginApplication;
            bool isWebViewSmooth = XConfig.PullTriggerPointerLock.Value == 4 && hovering.IsPluginApplication;
            if (!isDesktopOrCapture && !isWebViewLock && !isWebViewSmooth) return;

            float axisValue = GetTriggerAxis(__instance);
            Data.IsDown = ___HadMouseInputDown || ___HoldingTouch || ___IsWebViewTouchEventDown;

            if (XConfig.PullTriggerPointerLock.Value >= 3) // Smooth Mode
            {
                Data.IsDown = Data.IsDown || axisValue >= XConfig.PullTriggerClickThreshold.Value;

                if (axisValue > 0f)
                {
                    if (isWebViewSmooth)
                        hovering.UseCursorSmoothing = isWebViewSmooth;

                    if (Data.IsStopping)
                        Plugin.Instance.StopCoroutine(Data.Coroutine);

                    Data.IsStopping = false;
                    Data.WasSmooth = true;

                    EventBridge.Ref_Raycaster.InterpolationSpeed(__instance) = 1f;
                }
                else if (Data.WasSmooth && !Data.IsStopping)
                {
                    Data.IsStopping = true;
                    Data.Coroutine = Plugin.Instance.StartCoroutine(UnblockDelay(__instance));
                }
            }
            else
            {
                if (axisValue > 0f && !Data.IsDown) // Lock Mode
                {
                    if (Data.IsStopping)
                        Plugin.Instance.StopCoroutine(Data.Coroutine);

                    if (!Data.IsBlock)
                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                    Data.IsStopping = false;
                    Data.IsBlock = true;
                }
                else if (Data.IsBlock && !Data.IsStopping)
                {
                    if (!Data.IsDown)
                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                    Data.IsDown = false;
                    Data.IsStopping = true;
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
                if (Data.WasSmooth)
                    MouseSmoothSpeed.AppylySmoothSpeed(instance);

                Data.WasSmooth = false;
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