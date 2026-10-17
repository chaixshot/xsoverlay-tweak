using HarmonyLib;
using System;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class DoubleClickConfirm
    {
        public static readonly Action<Raycaster, bool> AnimateCursorHold = AccessTools.MethodDelegate<Action<Raycaster, bool>>(AccessTools.Method(typeof(Raycaster), "AnimateCursorHold"));

        private static Vector2 lastDesktopCoordinates = new();

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenDoubleClick()
        {
            Clicker.OnClick += (instance, ref desktopCoordinate, button, holdingTouch, isDoubleClickWin, isDoubleClickXSO) =>
            {
                if (!IsEnable()) return;

                // Cache the cursor position and set it back when double-clicking to avoid hand movement jitter
                if (isDoubleClickXSO)
                {
                    desktopCoordinate = lastDesktopCoordinates;
                    MouseOperations.SetCursorPosition((int)lastDesktopCoordinates.x, (int)lastDesktopCoordinates.y);
                    ExecuteDelayedDoubleClick(instance, button, holdingTouch);
                }
                else if (!holdingTouch)
                    lastDesktopCoordinates = desktopCoordinate;
            };
        }

        // Separate async helper method to handle the delay and execution without ref/out parameters
        private static async void ExecuteDelayedDoubleClick(Raycaster instance, int button, bool holdingTouch)
        {
            if (!holdingTouch) // Handle standard double-clicking (SendCapturedPressClick)
            {
                await Task.Delay(100);
                MouseOperations.SetCursorPosition((int)lastDesktopCoordinates.x, (int)lastDesktopCoordinates.y);
                switch (button)
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
            else // Handle double-clicking and hold (SendCapturedPressDown)
            {
                AnimateCursorHold(instance, true);

                switch (button)
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

        private static bool IsEnable()
        {
            return XConfig.DoubleClickConfirm.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
    }
}
