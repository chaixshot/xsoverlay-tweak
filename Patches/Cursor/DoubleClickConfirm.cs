using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using XSOverlay;

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
        public static void ListenForConfigChange()
        {
            wDoubleClickTime = GetDoubleClickTime() / 1000f;
        }

        [HarmonyPatch("HandleClicksForDesktopWindows")]
        [HarmonyPrefix]
        public static bool WaitToConfrimDoubleClick(
            Raycaster __instance,

            ClickActions clickActions,
            MouseInputDevice ___InputDevice,

            bool ___HoldingTouch,
            Vector2 ___DesktopCoordinates,
            Vector2 ___CachedTouchPosition,

            ref bool ___HadMouseInputDown,
            ref bool ___HadMouseRightInputDown,
            ref bool ___HadMouseMiddleInputDown
            )
        {
            if (!IsEnable()) return true;

            DoubleClickConfirmState DoubleClickState = InstanceState.GetOrCreateValue(__instance);

            if (___InputDevice.InputSource == clickActions.InputSource)
                if (!___HadMouseInputDown)
                    if (__instance.CanClickDesktopCursor)
                    {
                        float delay = Time.time - DoubleClickState.lastClickTime;
                        bool isDoubleClickXSO = false;
                        bool isDoubleClickWin = false;
                        bool isWDoubleClick = delay <= wDoubleClickTime;

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
                            MouseOperations.SetCursorPosition((int)lastDesktopCoordinates.x, (int)lastDesktopCoordinates.y);
                        else
                            if (!___HoldingTouch)
                                lastDesktopCoordinates = ___DesktopCoordinates;
                            else
                                lastDesktopCoordinates = ___CachedTouchPosition;

                        if (isDoubleClickXSO || isDoubleClickWin)
                        {
                            if (!clickActions.IsHoldingMouseClick)
                            {
                                // Handle standard clicks / double clicks
                                switch (clickActions.ActionIndex)
                                {
                                    case 0: // Left
                                        if (isDoubleClickXSO)
                                            XInputManager.sim.Mouse.LeftButtonDoubleClick();
                                        else
                                            XInputManager.sim.Mouse.LeftButtonClick();
                                        break;
                                    case 1: // Right
                                        if (isDoubleClickXSO)
                                            XInputManager.sim.Mouse.RightButtonDoubleClick();
                                        else
                                            XInputManager.sim.Mouse.RightButtonClick();
                                        break;
                                    case 2: // Middle
                                        if (isDoubleClickXSO)
                                            XInputManager.sim.Mouse.MiddleButtonClick();
                                        XInputManager.sim.Mouse.MiddleButtonClick();
                                        break;
                                }

                                return false;
                            }
                            else if (!clickActions.ShouldRelease)
                            {
                                // Handle Click & Hold
                                AnimateCursorHold(__instance, true);

                                switch (clickActions.ActionIndex)
                                {
                                    case 0: // Left
                                        ___HadMouseInputDown = true;
                                        if (isDoubleClickXSO)
                                            MouseOperations.LMouseClick(XInputManager.sim); // Fixes original XSO double-click quirk
                                        MouseOperations.LMouseDown(XInputManager.sim);
                                        break;

                                    case 1: // Right
                                        ___HadMouseRightInputDown = true;
                                        if (isDoubleClickXSO)
                                            MouseOperations.RMouseClick(XInputManager.sim);
                                        MouseOperations.RMouseDown(XInputManager.sim);
                                        break;

                                    case 2: // Middle
                                        ___HadMouseMiddleInputDown = true;
                                        if (isDoubleClickXSO)
                                            MouseOperations.MMouseClick(XInputManager.sim);
                                        MouseOperations.MMouseDown(XInputManager.sim);
                                        break;
                                }

                                return false;
                            }
                        }
                    }

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.DoubleClickConfirm.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
    }
}
