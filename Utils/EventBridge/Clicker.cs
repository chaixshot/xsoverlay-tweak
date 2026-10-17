using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Utils.EventBridge
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class Clicker
    {
        private class ClickState
        {
            public float lastClickTime = 0f;
            public Vector2 lastClickCoordinate = Vector2.zero;
        }

        public static float winDoubleClickTimeSeconds;
        public static Vector2 doubleClickBoxSize = new();

        private static readonly ConditionalWeakTable<Raycaster, ClickState> InstanceState = new();

        public delegate void DoubleClickDelegate(
            Raycaster instance,
            ref Vector2 desktopCoordinate,
            int button,
            bool holdingTouch,
            bool isDoubleClickWin,
            bool isDoubleClickXSO
        );

        public static event DoubleClickDelegate OnClick;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void GetWindowsDoubleClickDelay()
        {
            // Fetch timing limit in seconds directly via System.Windows.Forms
            winDoubleClickTimeSeconds = SystemInformation.DoubleClickTime / 1000f;

            // Fetch spatial bounding box size directly
            doubleClickBoxSize = new Vector2(
                SystemInformation.DoubleClickSize.Width,
                SystemInformation.DoubleClickSize.Height
            );
        }

        [HarmonyPatch("SendCapturedPressClick"), HarmonyPatch("SendCapturedPressDown")]
        [HarmonyPostfix]
        public static void CheckDoubleClick(System.Reflection.MethodBase __originalMethod, Raycaster __instance, ref Vector2 ___CapturedPressDesktopCoordinate, int ___CapturedPressButton)
        {
            if (EventBridge.IsOverlayWebView(__instance.HoveringOverlay)) return;

            ClickState clickState = InstanceState.GetOrCreateValue(__instance);

            float currentTime = Time.time;
            float delay = currentTime - clickState.lastClickTime;
            Vector2 currentCoord = ___CapturedPressDesktopCoordinate;
            bool holdingTouch = __originalMethod.Name == "SendCapturedPressDown";

            bool isDoubleClickWin = IsWindowsDoubleClick(delay, currentCoord, clickState);
            bool isDoubleClickXSO = IsXsoDoubleClick(isDoubleClickWin, delay, currentCoord, clickState, holdingTouch);

            if (isDoubleClickWin || isDoubleClickXSO)
                clickState.lastClickTime = 0f;
            else
            {
                clickState.lastClickTime = currentTime;
                clickState.lastClickCoordinate = currentCoord;
            }

            OnClick?.Invoke(__instance, ref ___CapturedPressDesktopCoordinate, ___CapturedPressButton, holdingTouch, isDoubleClickWin, isDoubleClickXSO);
        }

        private static bool IsWindowsDoubleClick(float delay, Vector2 currentCoord, ClickState clickState)
        {
            if (winDoubleClickTimeSeconds == 0f) GetWindowsDoubleClickDelay();
            if (doubleClickBoxSize == Vector2.zero) GetWindowsDoubleClickDelay();

            bool isWinWithinTime = delay <= winDoubleClickTimeSeconds;
            bool isWinWithinBox = Math.Abs(currentCoord.x - clickState.lastClickCoordinate.x) <= (doubleClickBoxSize.x / 2f) && Math.Abs(currentCoord.y - clickState.lastClickCoordinate.y) <= (doubleClickBoxSize.y / 2f);

            if (!isWinWithinBox)
                clickState.lastClickTime = 0f;

            return isWinWithinTime && isWinWithinBox;
        }

        private static bool IsXsoDoubleClick(bool isDoubleClickWin, float delay, Vector2 currentCoord, ClickState clickState, bool holdingTouch)
        {
            if (isDoubleClickWin) return false;

            if (winDoubleClickTimeSeconds == 0f) GetWindowsDoubleClickDelay();
            if (doubleClickBoxSize == Vector2.zero) GetWindowsDoubleClickDelay();

            float doubleClickDelay = XSettingsManager.Instance.Settings.DoubleClickDelay;

            // Double-clicking and hold add more allow time
            if (holdingTouch && delay <= doubleClickDelay + 0.3f)
                doubleClickDelay += 0.3f;

            bool isWinWithinTime = delay <= doubleClickDelay;
            bool isWinWithinBox = Math.Abs(currentCoord.x - clickState.lastClickCoordinate.x) <= (doubleClickBoxSize.x * 5f) && Math.Abs(currentCoord.y - clickState.lastClickCoordinate.y) <= (doubleClickBoxSize.y * 5f);

            if (!isWinWithinBox)
                clickState.lastClickTime = 0f;

            return isWinWithinTime && isWinWithinBox;
        }
    }
}
