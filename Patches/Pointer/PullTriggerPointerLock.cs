using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
            public bool IsLocking = false;
            public bool IsLockingOverThreshold = false;
            public bool IsReleasing = false;
            public bool IsDown = false;
            public bool WasSmooth = false;
            public bool WasClick = false;
            public Quaternion InitialLockRotation = new();
            public Coroutine ReleaseingCoroutine;
        }
        public static readonly ConditionalWeakTable<Raycaster, RaycasterState> InstanceState = new();

        private static readonly Func<Raycaster, float> GetTriggerAxis = AccessTools.MethodDelegate<Func<Raycaster, float>>(AccessTools.Method(typeof(Raycaster), "GetTriggerAxis"));

        private const float ANGLE_THRESHOLD = 1f;

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void Initiation(Raycaster __instance)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            InstanceState.Add(__instance, new());

            XConfig.PullTriggerPointerLock.SettingChanged += (sender, args) =>
            {
                if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                {
                    if (Data.ReleaseingCoroutine != null)
                        __instance.StopCoroutine(Data.ReleaseingCoroutine);

                    Data.IsReleasing = true;
                    Data.ReleaseingCoroutine = __instance.StartCoroutine(ReleaseingDelay(__instance));
                }
            };
        }

        [HarmonyPatch(typeof(Raycaster), "PreparePointerFrame")]
        [HarmonyPostfix]
        public static void ListenTriggerAxis(
            Raycaster __instance, bool ___HadMouseInputDown, bool ___HoldingTouch, bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsActiveHand(__instance)) return;
            if (!InstanceState.TryGetValue(__instance, out RaycasterState Data)) return;

            Unity_Overlay hovering = __instance.HoveringOverlay;
            if (hovering == null || hovering.IsHeld || hovering.IsLocked || __instance.HeldOverlay != null)
            {
                // Force reset state if hovering state was invalidated mid-pull
                if (Data.IsLocking || Data.WasSmooth)
                {
                    if (Data.ReleaseingCoroutine != null)
                        __instance.StopCoroutine(Data.ReleaseingCoroutine);
                    Data.ReleaseingCoroutine = __instance.StartCoroutine(ReleaseingDelay(__instance));
                }
                return;
            }

            bool isDesktopOrCapture = hovering.IsDesktopOrWindowCapture;
            bool isWebViewLock = XConfig.PullTriggerPointerLock.Value == 2 && hovering.IsPluginApplication;
            bool isWebViewSmooth = XConfig.PullTriggerPointerLock.Value == 4 && hovering.IsPluginApplication;
            if (!isDesktopOrCapture && !isWebViewLock && !isWebViewSmooth) return;

            float axisValue = GetTriggerAxis(__instance);
            Data.IsDown = ___HadMouseInputDown || ___HoldingTouch || ___IsWebViewTouchEventDown;

            if (IsSmoothMode())
            {
                Data.IsDown = Data.IsDown || axisValue >= XConfig.PullTriggerClickThreshold.Value;

                if (axisValue > 0f) // Start Pull
                {
                    if (isWebViewSmooth)
                        hovering.UseCursorSmoothing = isWebViewSmooth;

                    if (Data.IsReleasing && Data.ReleaseingCoroutine != null)
                        __instance.StopCoroutine(Data.ReleaseingCoroutine);

                    float currentSetting = Mathf.Clamp01(XSettingsManager.Instance.Settings.PointerSmoothing);
                    if (currentSetting <= 0f)
                        XSettingsManager.Instance.Settings.PointerSmoothing = 0.1f;

                    Data.WasSmooth = true;
                    Data.IsReleasing = false;
                }
                else if (Data.WasSmooth && !Data.IsReleasing) // Releasing Pull
                {
                    Data.IsReleasing = true;
                    Data.ReleaseingCoroutine = __instance.StartCoroutine(ReleaseingDelay(__instance));
                }
            }
            else if (IsLockMode())
            {
                if (axisValue > 0f && !Data.IsDown) // Start Pull
                {
                    if (Data.IsReleasing && Data.ReleaseingCoroutine != null)
                        __instance.StopCoroutine(Data.ReleaseingCoroutine);

                    if (!Data.IsLocking)
                    {
                        Data.InitialLockRotation = __instance.transform.rotation;

                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);
                    }
                    Data.IsReleasing = false;
                    Data.IsLocking = true;
                }
                else if (Data.IsLocking && !Data.IsReleasing) // Releasing Pull
                {
                    if (!Data.IsDown)
                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                    Data.IsDown = false;
                    Data.IsReleasing = true;
                    Data.ReleaseingCoroutine = __instance.StartCoroutine(ReleaseingDelay(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPrefix]
        public static bool FreezeSearchForOverlays(Raycaster __instance)
        {
            if (!IsLockMode()) return true;
            if (EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay)) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                if (Data.IsLocking)
                {
                    float angleDelta = Quaternion.Angle(__instance.transform.rotation, Data.InitialLockRotation);

                    return Data.IsLockingOverThreshold = !Data.WasClick && angleDelta > ANGLE_THRESHOLD;
                }

            return true;
        }

        [HarmonyPatch(typeof(MouseInputDevice), nameof(MouseInputDevice.StartClickFreezePeriod))]
        [HarmonyPrefix]
        public static bool BlockOriginalDoubleClickDelay()
        {
            return !IsEnable();
        }

        [HarmonyPatch(typeof(Raycaster), "AnimateCursorClick")]
        [HarmonyPatch(typeof(Raycaster), "AnimateCursorHold")]
        [HarmonyPrefix]
        public static void ListenClickInput(Raycaster __instance)
        {
            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                Data.WasClick = true;
        }

        [HarmonyPatch(typeof(Raycaster), "CheckOverlayIntersection")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CustomSmoothValue(IEnumerable<CodeInstruction> instructions)
        {
            bool patchedLerp = false;
            List<CodeInstruction> codes = [.. instructions];

            MethodInfo mathfLerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), [typeof(float), typeof(float), typeof(float)]);
            MethodInfo customSmoothing = AccessTools.Method(typeof(PullTriggerPointerLock), nameof(CalculateSmoothValue), [typeof(float), typeof(float), typeof(float), typeof(Raycaster)]);

            for (int i = 0; i < codes.Count; i++)
            {
                // Replace Mathf.Lerp with custom dynamic smoothing getter
                if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == mathfLerp)
                {
                    // Insert ldarg.0 to push 'this' (Raycaster instance) onto the stack before the call
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    codes[i + 1] = new CodeInstruction(OpCodes.Call, customSmoothing);

                    patchedLerp = true;
                    i++; // Skip the newly inserted instruction
                }
            }

            if (!patchedLerp)
                Plugin.Logger.LogError($"PointerSmoothing patch failed (Lerp: {patchedLerp}). The mod may be outdated.");

            return codes;
        }

        private static float CalculateSmoothValue(float unusedA, float unusedB, float unusedC, Raycaster instance)
        {
            if (!IsSmoothMode()) return Mathf.Lerp(unusedA, unusedB, unusedC);

            if (instance != null && InstanceState.TryGetValue(instance, out RaycasterState Data))
            {
                float power = 15f;
                float smoothing = Mathf.Clamp01(XSettingsManager.Instance.Settings.PointerSmoothing);

                if (Data.WasSmooth)
                {
                    power = 1.5f;
                    smoothing = 1f;

                    if (Data.IsDown)
                        smoothing = 0.9f;
                }
                else
                    return Mathf.Lerp(unusedA, unusedB, unusedC);

                float maxSmoothWeight = Mathf.Clamp01(Time.deltaTime * power);
                return Mathf.Lerp(1f, maxSmoothWeight, smoothing);
            }

            return 1f;
        }

        private static IEnumerator ReleaseingDelay(Raycaster instance)
        {
            if (InstanceState.TryGetValue(instance, out RaycasterState Data))
            {
                yield return new WaitForSecondsRealtime(Data.WasClick ? XSettingsManager.Instance.Settings.DoubleClickDelay : 0f);

                Data.IsLocking = false;
                Data.IsLockingOverThreshold = false;
                Data.IsReleasing = false;
                Data.IsDown = false;
                Data.WasSmooth = false;
                Data.WasClick = false;
            }
        }

        public static bool ShouldLockPointer(Raycaster instance)
        {
            if (InstanceState.TryGetValue(instance, out RaycasterState Data))
                if (Data.IsLocking == true && Data.IsLockingOverThreshold == false)
                    return true;

            return false;
        }

        private static bool IsSmoothMode() => XConfig.PullTriggerPointerLock.Value is 3 or 4;
        private static bool IsLockMode() => XConfig.PullTriggerPointerLock.Value is 1 or 2;
        private static bool IsEnable() => XConfig.PullTriggerPointerLock.Value != 0;
    }
}