using HarmonyLib;
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

            ref ClickActions clickActions,
            ref MouseInputDevice ___InputDevice,
            ref bool ___HadMouseInputDown,

            bool ___HoldingTouch,
            Vector2 ___DesktopCoordinates,
            Vector2 ___CachedTouchPosition
            )
        {
            if (!IsEnable()) return true;

            DoubleClickConfirmState DoubleClickState = InstanceState.GetOrCreateValue(__instance);

            if (___InputDevice.InputSource == clickActions.InputSource)
                if (!___HadMouseInputDown)
                    if (__instance.CanClickDesktopCursor)
                        if (!clickActions.IsHoldingMouseClick)
                        {
                            float delay = Time.time - DoubleClickState.lastClickTime;
                            bool isDoubleClick = false;
                            bool isWDoubleClick = delay <= wDoubleClickTime;

                            if (!isWDoubleClick && delay <= XSettingsManager.Instance.Settings.DoubleClickDelay)
                            {
                                isDoubleClick = true;
                                DoubleClickState.lastClickTime = 0f;
                            }
                            else if (isWDoubleClick)
                            {
                                isDoubleClick = true;
                                DoubleClickState.lastClickTime = 0f;
                            }
                            else
                                DoubleClickState.lastClickTime = Time.time;

                            // Cache the cursor position and set it back when double-click to avoid the cursor moving from hand movement between clicks
                            if (isDoubleClick)
                                MouseOperations.SetCursorPosition((int)lastDesktopCoordinates.x, (int)lastDesktopCoordinates.y);
                            else
                                if (!___HoldingTouch)
                                    lastDesktopCoordinates = ___DesktopCoordinates;
                                else
                                    lastDesktopCoordinates = ___CachedTouchPosition;

                            if (isDoubleClick)
                            {
                                switch (clickActions.ActionIndex)
                                {
                                    case 0:
                                        XInputManager.sim.Mouse.LeftButtonDoubleClick();
                                        break;
                                    case 1:
                                        XInputManager.sim.Mouse.RightButtonDoubleClick();
                                        break;
                                    case 2:
                                        MouseOperations.MMouseClick(XInputManager.sim);
                                        MouseOperations.MMouseClick(XInputManager.sim);
                                        break;
                                }

                                return false;
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
