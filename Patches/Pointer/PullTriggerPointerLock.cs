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
            public bool IsBlock = false;
            public bool IsStopping = false;
            public bool IsDown = false;
            public bool WasSmooth = false;
            public bool WasClick = false;
            public Vector2 DesktopCoordinates = new();
            public Coroutine Coroutine;
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
                    if (Data.Coroutine != null)
                        __instance.StopCoroutine(Data.Coroutine);

                    Data.IsStopping = true;
                    Data.Coroutine = __instance.StartCoroutine(UnblockDelay(__instance));
                }
            };
        }

        [HarmonyPatch(typeof(Raycaster), "PreparePointerFrame")]
        [HarmonyPostfix]
        public static void ListenTriggerAxis(Raycaster __instance, bool ___HadMouseInputDown, bool ___HoldingTouch, bool ___IsWebViewTouchEventDown)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsActiveHand(__instance)) return;
            if (!InstanceState.TryGetValue(__instance, out RaycasterState Data)) return;

            Unity_Overlay hovering = __instance.HoveringOverlay;
            if (hovering == null || hovering.IsHeld || hovering.IsLocked || __instance.HeldOverlay != null)
            {
                // Force reset state if hovering state was invalidated mid-pull
                if (Data.IsBlock || Data.WasSmooth)
                {
                    if (Data.Coroutine != null)
                        __instance.StopCoroutine(Data.Coroutine);
                    Data.Coroutine = __instance.StartCoroutine(UnblockDelay(__instance));
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

                    if (Data.IsStopping && Data.Coroutine != null)
                        __instance.StopCoroutine(Data.Coroutine);

                    if (defaultSmoothing <= 0.85f) // Do smooth when pulling, less smooth when click holding
                        XSettingsManager.Instance.Settings.PointerSmoothing = Data.IsDown ? 0.85f : 1.0f;

                    Data.WasSmooth = true;
                    Data.IsStopping = false;
                }
                else if (Data.WasSmooth && !Data.IsStopping) // Releasing Pull
                {
                    Data.IsStopping = true;
                    Data.Coroutine = __instance.StartCoroutine(UnblockDelay(__instance));
                }
            }
            else if (IsLockMode())
            {
                if (axisValue > 0f && !Data.IsDown) // Start Pull
                {
                    if (Data.IsStopping && Data.Coroutine != null)
                        __instance.StopCoroutine(Data.Coroutine);

                    if (!Data.IsBlock)
                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                    Data.IsStopping = false;
                    Data.IsBlock = true;
                }
                else if (Data.IsBlock && !Data.IsStopping) // Releasing Pull
                {
                    if (!Data.IsDown)
                        AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40f, XConfig.PullTriggerPointerLockHaptic.Value / 100f);

                    Data.IsDown = false;
                    Data.IsStopping = true;
                    Data.Coroutine = __instance.StartCoroutine(UnblockDelay(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(Raycaster), "PointerHoverAndStateManagement")]
        [HarmonyPrefix]
        public static void BlockCursorMovement(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsLockMode()) return;

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
            if (!IsLockMode()) return true;
            if (EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay)) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPrefix]
        public static bool BlockSearchForOverlays(Raycaster __instance)
        {
            if (!IsLockMode()) return true;
            if (EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay)) return true;

            if (InstanceState.TryGetValue(__instance, out RaycasterState Data))
                return !Data.IsBlock;

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows"), HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static void InputClickLockPosition(Raycaster __instance, ref Vector2 ___DesktopCoordinates)
        {
            if (!IsLockMode()) return;

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

        [HarmonyPatch(typeof(Raycaster), "HandleClicksForDesktopWindows")]
        [HarmonyPatch(typeof(Raycaster), "HandleTouchInputForDesktopWindows")]
        [HarmonyPatch(typeof(Raycaster), "HandleHeadWebAppInput")]
        [HarmonyPatch(typeof(Raycaster), "BeginWebViewTouch")]
        [HarmonyPatch(typeof(Raycaster), "BeginWebViewSinglePointer")]
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

        private static IEnumerator UnblockDelay(Raycaster instance)
        {
            if (InstanceState.TryGetValue(instance, out RaycasterState Data))
            {
                yield return new WaitForSecondsRealtime(Data.WasClick ? XSettingsManager.Instance.Settings.DoubleClickDelay : 0f);

                if (Data.WasSmooth) // Restore smoothing when released
                    XSettingsManager.Instance.Settings.PointerSmoothing = defaultSmoothing;

                Data.IsBlock = false;
                Data.IsStopping = false;
                Data.WasClick = false;
                Data.WasSmooth = false;
            }
        }

        private static bool IsSmoothMode() => XConfig.PullTriggerPointerLock.Value is 3 or 4;
        private static bool IsLockMode() => XConfig.PullTriggerPointerLock.Value is 1 or 2;
        private static bool IsEnable() => XConfig.PullTriggerPointerLock.Value != 0;
    }
}