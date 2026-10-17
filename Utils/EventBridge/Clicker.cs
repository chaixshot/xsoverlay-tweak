using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;

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

        public static int WinDoubleClickTime = 0;
        private static Vector2 doubleClickBoxSize = new();

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
            WinDoubleClickTime = SystemInformation.DoubleClickTime;

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

            if (WinDoubleClickTime == 0) GetWindowsDoubleClickDelay();
            if (doubleClickBoxSize == Vector2.zero) GetWindowsDoubleClickDelay();

            ClickState clickState = InstanceState.GetOrCreateValue(__instance);

            float currentTime = Time.time;
            float delay = currentTime - clickState.lastClickTime;
            Vector2 currentCoord = ___CapturedPressDesktopCoordinate;
            bool holdingTouch = __originalMethod.Name == "SendCapturedPressDown";

            bool isWinWithinBox = Math.Abs(currentCoord.x - clickState.lastClickCoordinate.x) <= (doubleClickBoxSize.x * 5f) && Math.Abs(currentCoord.y - clickState.lastClickCoordinate.y) <= (doubleClickBoxSize.y * 5f);
            bool isDoubleClickWin = false;
            bool isDoubleClickXSO = false;

            if (isWinWithinBox)
            {
                isDoubleClickWin = IsWindowsDoubleClick(delay, currentCoord, clickState);

                if (!isDoubleClickWin)
                    isDoubleClickXSO = IsXsoDoubleClick(delay, holdingTouch);
            }

            if (isDoubleClickWin || isDoubleClickXSO)
                clickState.lastClickTime = 0f;
            else
            {
                clickState.lastClickTime = currentTime;
                clickState.lastClickCoordinate = currentCoord;
            }

            OnClick?.Invoke(__instance, ref ___CapturedPressDesktopCoordinate, ___CapturedPressButton, holdingTouch, isDoubleClickWin, isDoubleClickXSO);
        }

        // Check is Windows default double-click triggered
        private static bool IsWindowsDoubleClick(float delay, Vector2 currentCoord, ClickState clickState)
        {
            bool isWinWithinTime = delay <= (WinDoubleClickTime / 1000f);
            bool isWinWithinBox = DoubleClickConfirm.IsEnable();

            if (!isWinWithinBox)
                isWinWithinBox = Math.Abs(currentCoord.x - clickState.lastClickCoordinate.x) <= (doubleClickBoxSize.x) && Math.Abs(currentCoord.y - clickState.lastClickCoordinate.y) <= (doubleClickBoxSize.y);

            return isWinWithinTime && isWinWithinBox;
        }

        private static bool IsXsoDoubleClick(float delay, bool holdingTouch)
        {
            if (!DoubleClickConfirm.IsEnable()) return false;

            float doubleClickDelay = XSettingsManager.Instance.Settings.DoubleClickDelay;

            // Double-clicking and hold add more allow time
            if (holdingTouch && delay <= doubleClickDelay + 0.3f)
                doubleClickDelay += 0.3f;

            bool isWinWithinTime = delay <= doubleClickDelay;

            return isWinWithinTime;
        }
    }
}
