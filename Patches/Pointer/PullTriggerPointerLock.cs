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
        public static void ListenTriggerAxis(Raycaster __instance, bool ___PressTargetDownSent, int ___PressedMouseButton, bool ___IsWebViewTouchEventDown)
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
            bool isWebViewLock = XConfig.PullTriggerPointerLock.Value == 2 && hovering.IsWebApplication;
            bool isWebViewSmooth = XConfig.PullTriggerPointerLock.Value == 4 && hovering.IsWebApplication;
            bool holdingTouch = ___PressedMouseButton != -1;

            if (!isDesktopOrCapture && !isWebViewLock && !isWebViewSmooth) return;
            if (EventBridge.IsOverlayKeyboard(hovering)) return;

            float axisValue = GetTriggerAxis(__instance);
            Data.IsDown = ___PressTargetDownSent || holdingTouch || ___IsWebViewTouchEventDown;

            if (IsSmoothMode())
            {
                Data.IsDown = Data.IsDown || axisValue >= XConfig.PullTriggerClickThreshold.Value;

                if (axisValue > 0f) // Start Pull
                {
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
                    if (Data.IsLockingOverThreshold = !Data.WasClick && angleDelta > ANGLE_THRESHOLD)
                        return false;
                }

            return true;
        }

        [HarmonyPatch(typeof(MouseInputDevice), nameof(MouseInputDevice.StartClickFreezePeriod))]
        [HarmonyPrefix]
        public static bool BlockOriginalDoubleClickDelay()
        {
            return !IsEnable();
        }

        [HarmonyPatch(typeof(Raycaster), "SendCapturedPressClick"), HarmonyPatch(typeof(Raycaster), "SendCapturedPressDown")]
        [HarmonyPrefix]
        public static void ListenClickInput(Raycaster __instance, bool __result)
        {
            if (!__result) return;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                Data.WasClick = true;
        }

        [HarmonyPatch(typeof(Raycaster), "CheckOverlayIntersection")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CustomSmoothValue(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            bool patched = false;

            MethodInfo customGetSmoothing = AccessTools.Method(typeof(PullTriggerPointerLock), nameof(GetCustomPointerSmoothing), [typeof(float), typeof(Raycaster)]);

            // Look for the assignment to local variable 'pointerSmoothing' right before the zero check: if (pointerSmoothing == 0f)
            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i + 3].opcode == OpCodes.Ldc_R4 && (float)codes[i + 3].operand == 0f
                    && (codes[i + 1].opcode == OpCodes.Stloc || codes[i + 1].opcode == OpCodes.Stloc_S
                        || codes[i + 1].opcode == OpCodes.Stloc_1 || codes[i + 1].opcode == OpCodes.Stloc_2 || codes[i + 1].opcode == OpCodes.Stloc_3))
                {
                    // Insert before stloc: Stack currently has original float on top
                    // Push 'this' (Raycaster instance)
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    // Call custom smoothing modifier
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, customGetSmoothing));

                    patched = true;
                    break;
                }
            }

            if (!patched)
                Plugin.Logger.LogError("[XSOverlay Tweak] PointerSmoothing transpiler patch failed! Target IL sequence not found.");

            return codes;
        }

        private static float GetCustomPointerSmoothing(float originalSmoothing, Raycaster instance)
        {
            if (!IsSmoothMode() || instance == null || EventBridge.IsOverlayKeyboard(instance.HoveringOverlay))
            {
                return originalSmoothing;
            }

            if (InstanceState.TryGetValue(instance, out RaycasterState data))
                if (data.WasSmooth)
                    return data.IsDown ? 5.0f : 20.0f;

            return originalSmoothing;
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