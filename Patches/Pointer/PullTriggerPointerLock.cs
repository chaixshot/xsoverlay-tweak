using HarmonyLib;
using System;
using System.Collections;
using System.Globalization;
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
        private static float defaultSmoothing = XSettingsManager.Instance.Settings.PointerSmoothing;

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

                    if (defaultSmoothing <= 0.85f) // Do smooth when pulling, less smooth when click holding
                        XSettingsManager.Instance.Settings.PointerSmoothing = Data.IsDown ? 0.85f : 1.0f;

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
                    float ANGLE_THRESHOLD = 1f * EventBridge.OneDegree;

                    Data.IsLockingOverThreshold = !Data.WasClick && angleDelta > ANGLE_THRESHOLD;

                    return Data.IsLockingOverThreshold;
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

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPostfix]
        public static void ListenSettingChanged(string name, string value, string value1, bool sendAnalytics = true)
        {
            if (name.Equals("PointerSmoothing"))
                defaultSmoothing = Mathf.Clamp01(float.Parse(value, CultureInfo.InvariantCulture));
        }

        private static IEnumerator ReleaseingDelay(Raycaster instance)
        {
            if (InstanceState.TryGetValue(instance, out RaycasterState Data))
            {
                yield return new WaitForSecondsRealtime(Data.WasClick ? XSettingsManager.Instance.Settings.DoubleClickDelay : 0f);

                if (Data.WasSmooth) // Restore smoothing when released
                    XSettingsManager.Instance.Settings.PointerSmoothing = defaultSmoothing;

                Data.IsLocking = false;
                Data.IsLockingOverThreshold = false;
                Data.IsReleasing = false;
                Data.IsDown = false;
                Data.WasSmooth = false;
                Data.WasClick = false;
            }
        }

        private static bool IsSmoothMode() => XConfig.PullTriggerPointerLock.Value is 3 or 4;
        private static bool IsLockMode() => XConfig.PullTriggerPointerLock.Value is 1 or 2;
        private static bool IsEnable() => XConfig.PullTriggerPointerLock.Value != 0;
    }
}