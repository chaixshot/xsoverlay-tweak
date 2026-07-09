using HarmonyLib;
using System.Runtime.InteropServices;
using WindowsInput;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Mouse
{
    internal class MouseButtonSwap
    {
        private static bool IsMouseSwap = false;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void GetMouseSwapState()
        {
            IsMouseSwap = IsMouseSwapped();
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.LMouseClick))]
        [HarmonyPrefix]
        public static bool SwapClickLeftToRight(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.RightButtonClick();
            return false;
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.LMouseDown))]
        [HarmonyPrefix]
        public static bool SwapDownLeftToRight(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.RightButtonDown();
            return false;
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.LMouseUp))]
        [HarmonyPrefix]
        public static bool SwapUpLeftToRight(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.RightButtonUp();
            return false;
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.RMouseClick))]
        [HarmonyPrefix]
        public static bool SwapClickRightToLeft(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.LeftButtonClick();
            return false;
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.RMouseDown))]
        [HarmonyPrefix]
        public static bool SwapDownRightToLeft(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.LeftButtonDown();
            return false;
        }

        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.RMouseUp))]
        [HarmonyPrefix]
        public static bool SwapUpRightToLeft(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            sim.Mouse.LeftButtonUp();
            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.MouseButtonSwap.Value && IsMouseSwap;
        }

        // Import GetSystemMetrics from user32.dll
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        // The system metric constant for swapped mouse buttons
        private const int SM_SWAPBUTTON = 23;

        /// <summary>
        /// Checks if the primary and secondary mouse buttons are swapped.
        /// </summary>
        /// <returns>True if swapped (Right-click is Primary), False if default (Left-click is Primary)</returns>
        public static bool IsMouseSwapped()
        {
            // GetSystemMetrics returns non-zero if the buttons are swapped
            return GetSystemMetrics(SM_SWAPBUTTON) != 0;
        }
    }
}
