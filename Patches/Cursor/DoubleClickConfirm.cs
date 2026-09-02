using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class DoubleClickConfirm
    {
        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private class DoubleClickConfirmState
        {
            public float lastClickTime = 0f;
        }
        private static readonly ConditionalWeakTable<Raycaster, DoubleClickConfirmState> InstanceState = new();

        public static readonly Action<Raycaster, bool> AnimateCursorHold = AccessTools.MethodDelegate<Action<Raycaster, bool>>(AccessTools.Method(typeof(Raycaster), "AnimateCursorHold"));
        static float wDoubleClickTime;
        static Vector2 lastDesktopCoordinates;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void GetWindowsDoubleClickDelay()
        {
            wDoubleClickTime = GetDoubleClickTime() / 1000f;
        }

        [HarmonyPatch("SendCapturedPressClick"), HarmonyPatch("SendCapturedPressDown")]
        [HarmonyPostfix]
        public static void WaitToConfrimDoubleClick(System.Reflection.MethodBase __originalMethod, Raycaster __instance, ref Vector2 ___CapturedPressDesktopCoordinate, int ___CapturedPressButton)
        {
            if (!IsEnable()) return;
            if (EventBridge.IsOverlayWebView(__instance.HoveringOverlay)) return;

            DoubleClickConfirmState DoubleClickState = InstanceState.GetOrCreateValue(__instance);

            float delay = Time.time - DoubleClickState.lastClickTime;
            bool isDoubleClickXSO = false;
            bool isDoubleClickWin = false;
            bool isWDoubleClick = delay <= wDoubleClickTime;
            bool holdingTouch = __originalMethod.Name == "SendCapturedPressDown";

            if (!isWDoubleClick && delay <= XSettingsManager.Instance.Settings.DoubleClickDelay)
            {
                isDoubleClickXSO = true;
                DoubleClickState.lastClickTime = 0f;
            }
            else if (isWDoubleClick)
            {
                isDoubleClickWin = true;
                DoubleClickState.lastClickTime = 0f;
            }
            else
                DoubleClickState.lastClickTime = Time.time;

            // Cache the cursor position and set it back when double-click to avoid the cursor moving from hand movement between clicks
            if (isDoubleClickXSO || isDoubleClickWin)
            {
                ___CapturedPressDesktopCoordinate = lastDesktopCoordinates;
                MouseOperations.SetCursorPosition((int)___CapturedPressDesktopCoordinate.x, (int)___CapturedPressDesktopCoordinate.y);
            }
            else if (!holdingTouch)
                lastDesktopCoordinates = ___CapturedPressDesktopCoordinate;

            if (!isDoubleClickWin && isDoubleClickXSO)
            {
                if (!holdingTouch) // Handle standard clicks - double clicks (SendCapturedPressClick)
                {
                    switch (___CapturedPressButton)
                    {
                        case 0: // Left
                            InputManager.sim.Mouse.LeftButtonDoubleClick();
                            break;
                        case 1: // Right
                            InputManager.sim.Mouse.RightButtonDoubleClick();
                            break;
                        case 2: // Middle
                            InputManager.sim.Mouse.MiddleButtonClick();
                            InputManager.sim.Mouse.MiddleButtonClick();
                            break;
                    }
                }
                else // Handle Click - Relase - Click Hold (SendCapturedPressDown)
                {
                    AnimateCursorHold(__instance, true);

                    switch (___CapturedPressButton)
                    {
                        case 0: // Left
                            MouseOperations.LMouseUp(InputManager.sim);
                            MouseOperations.LMouseDown(InputManager.sim);
                            break;

                        case 1: // Right
                            MouseOperations.RMouseUp(InputManager.sim);
                            MouseOperations.RMouseDown(InputManager.sim);
                            break;

                        case 2: // Middle
                            MouseOperations.MMouseUp(InputManager.sim);
                            MouseOperations.MMouseDown(InputManager.sim);
                            break;

                    }
                }
            }
        }

        private static bool IsEnable()
        {
            return XConfig.DoubleClickConfirm.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
    }
}
