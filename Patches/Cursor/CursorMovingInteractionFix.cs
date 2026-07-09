using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class CursorMovingInteractionFix
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private static float lastTriggerTime;
        private static int lastX;
        private static int lastY;

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.SetCursorPosition))]
        [HarmonyPostfix]
        public static void SetCursorPosition(int x, int y)
        {
            if (!IsEnable()) return;

            if (Time.unscaledTime - lastTriggerTime >= 0.05f) // ~20 FPS
            {
                if (Math.Abs(x - lastX) > 20 || Math.Abs(y - lastY) > 20)
                {
                    lastTriggerTime = Time.unscaledTime;
                    mouse_event(MOUSEEVENTF_MOVE, 1, 1, 0, 0);

                    lastX = x;
                    lastY = y;
                }
            }
        }

        private static bool IsEnable()
        {
            return XConfig.CursorMovingInteractionFix.Value;
        }
    }
}