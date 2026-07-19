using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Optimization
{
    internal class EfficiencyMode
    {
        public static bool IsInEfficiencyMode = false;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void InitializeEvents(DeviceManager __instance)
        {
            // Listen to edit mode change
            XSOEventSystem.OnToggleLayoutMode += (isEditMode) =>
            {
                if (IsEfficiencyModeEnable())
                    if (isEditMode) // Smooth overlay fadeout
                        EventBridge.Ref_DeviceManager.GetHMDRefreshRate(__instance);
            };
        }

        [HarmonyPatch(typeof(DeviceManager), "GetHMDRefreshRate")]
        [HarmonyPostfix]
        public static void DetermineShouldEfficiencyMode()
        {
            if (ShouldInEfficiencyMode())
            {
                IsInEfficiencyMode = true;
                Application.runInBackground = false;
                EfficiencyModeController.SetEfficiencyMode(true);
            }
            else if (IsInEfficiencyMode)
            {
                IsInEfficiencyMode = false;
                Application.runInBackground = true;
                EfficiencyModeController.SetEfficiencyMode(false);
            }
        }

        [HarmonyPatch(typeof(Unity_Overlay), "FadeOverlayIn")]
        [HarmonyPostfix]
        public static void OverlayPinnedFadedIn(Unity_Overlay __instance)
        {
            if (XConfig.EfficiencyMode.Value.Equals(2))
                if (__instance.isPinned)
                    Plugin.Instance.StartCoroutine(Delay());

            static IEnumerator Delay()
            {
                yield return new WaitForSecondsRealtime(0.1f);
                EventBridge.Ref_DeviceManager.GetHMDRefreshRate(DeviceManager.Instance);
            }
        }

        public static bool IsEfficiencyModeEnable()
        {
            return !XConfig.EfficiencyMode.Value.Equals(0);
        }

        public static bool ShouldInEfficiencyMode()
        {
            bool inNonActive = IsEfficiencyModeEnable() && !Overlay_Manager.Instance.editMode && !EventBridge.IsHoverAnyOverlay() && !EventBridge.IsNotificationVisible;

            if (XConfig.EfficiencyMode.Value.Equals(1))
                return inNonActive;
            else if (XConfig.EfficiencyMode.Value.Equals(2))
            {
                bool isPinnedVisible = false;
                List<Unity_Overlay> activeOverlayComponents = Overlay_Manager.Instance.ActiveOverlayComponents;

                for (int i = 0; i < activeOverlayComponents.Count; i++)
                {
                    Unity_Overlay overlay = activeOverlayComponents[i];

                    if (!overlay.opacity.Equals(0) && overlay.isPinned && !overlay.IsHidden && !overlay.IsPaused)
                        isPinnedVisible = true;
                }

                return inNonActive && !isPinnedVisible;
            }

            return false;
        }
    }

    internal class EfficiencyModeController
    {
        private static readonly IntPtr ProcessHandle = GetCurrentProcess();

        // --- Windows API Imports ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess,
            int ProcessInformationClass,
            ref PROCESS_POWER_THROTTLING_STATE ProcessInformation,
            uint ProcessInformationSize
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

        // --- Windows API Constants ---
        private const int ProcessPowerThrottling = 4;
        private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED_RELIABILITY = 0x1;

        // Priority Classes
        private const uint NORMAL_PRIORITY_CLASS = 0x00000020;
        private const uint IDLE_PRIORITY_CLASS = 0x00000040;

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        /// <summary>
        /// Set the efficiency mode state for the current process.
        /// </summary>
        /// <param name="enable">True to turn Efficiency Mode ON (throttle), False to turn it OFF (performance).</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public static bool SetEfficiencyMode(bool enable)
        {
            // Safety check to ensure we are running on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            // 1. Configure EcoQoS (Power Throttling)
            PROCESS_POWER_THROTTLING_STATE throttlingState = new()
            {
                Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED_RELIABILITY,
                // If enabling, turn execution speed management ON. If disabling, turn it OFF.
                StateMask = enable ? PROCESS_POWER_THROTTLING_EXECUTION_SPEED_RELIABILITY : 0
            };

            uint structureSize = (uint)Marshal.SizeOf(throttlingState);
            bool powerResult = SetProcessInformation(ProcessHandle, ProcessPowerThrottling, ref throttlingState, structureSize);

            // 2. Configure Process Priority Class
            // Windows drops a process to Idle priority when Efficiency Mode is turned on.
            uint priorityClass = enable ? IDLE_PRIORITY_CLASS : NORMAL_PRIORITY_CLASS;
            bool priorityResult = SetPriorityClass(ProcessHandle, priorityClass);

            return powerResult && priorityResult;
        }
    }
}
