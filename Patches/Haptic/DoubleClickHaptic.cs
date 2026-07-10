using HarmonyLib;
using UnityEngine;
using WindowsInput;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class DoubleClickHaptic
    {
        private static float wDoubleClickTime; // Windows setting

        private static float lastLeftClickTime;
        private static float lastRightClickTime;
        private static float lastMiddleClickTime;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForConfigChange()
        {
            wDoubleClickTime = GetDoubleClickTime() / 1000f;
        }

        [HarmonyPatch(typeof(MouseSimulator), "LeftButtonDoubleClick")]
        [HarmonyPrefix]
        public static void Left_DoubleClick()
        {
            if (!IsEnable()) return;

            PlayDoubleClickHaptic();
        }

        [HarmonyPatch(typeof(MouseSimulator), "RightButtonDoubleClick")]
        [HarmonyPrefix]
        public static void Right_DoubleClick()
        {
            if (!IsEnable()) return;

            PlayDoubleClickHaptic();
        }

        [HarmonyPatch(typeof(MouseSimulator), "LeftButtonClick"), HarmonyPatch(typeof(MouseSimulator), "LeftButtonDown")]
        [HarmonyPrefix]
        public static void LeftClick()
        {
            if (!IsEnable()) return;

            float currentTime = Time.time;
            float delay = currentTime - lastLeftClickTime;

            if (delay <= wDoubleClickTime)
            {
                PlayDoubleClickHaptic();
                lastLeftClickTime = 0f;
            }
            else
                lastLeftClickTime = currentTime;
        }

        [HarmonyPatch(typeof(MouseSimulator), "RightButtonClick"), HarmonyPatch(typeof(MouseSimulator), "RightButtonDown")]
        [HarmonyPrefix]
        public static void RightClick()
        {
            if (!IsEnable()) return;

            float currentTime = Time.time;
            float delay = currentTime - lastRightClickTime;

            if (delay <= wDoubleClickTime)
            {
                PlayDoubleClickHaptic();
                lastRightClickTime = 0f;
            }
            else
                lastRightClickTime = currentTime;
        }

        [HarmonyPatch(typeof(MouseSimulator), "MiddleButtonClick"), HarmonyPatch(typeof(MouseSimulator), "MiddleButtonDown")]
        [HarmonyPrefix]
        public static void MiddleClick()
        {
            if (!IsEnable()) return;

            float currentTime = Time.time;
            float delay = currentTime - lastMiddleClickTime;

            if (delay <= wDoubleClickTime)
            {
                PlayDoubleClickHaptic();
                lastMiddleClickTime = 0f;
            }
            else
                lastMiddleClickTime = currentTime;
        }

        private static void PlayDoubleClickHaptic()
        {
            AdvancedHaptics.Rumble((DesktopCursorManager.Instance.GetCurrentInputDevice().HapticDeviceName == Raycaster.HapticDevice.Left), 0.05f, 40f, XConfig.DoubleClickHaptic.Value / 100f);
        }

        private static bool IsEnable()
        {
            return XConfig.DoubleClickHaptic.Value != 0;
        }
    }
}
