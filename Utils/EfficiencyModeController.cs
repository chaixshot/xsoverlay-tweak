using System;
using System.Runtime.InteropServices;

namespace xsoverlay_tweak.Utils
{
    internal class EfficiencyModeController
    {
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

            IntPtr hProcess = GetCurrentProcess();

            // 1. Configure EcoQoS (Power Throttling)
            PROCESS_POWER_THROTTLING_STATE throttlingState = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED_RELIABILITY,
                // If enabling, turn execution speed management ON. If disabling, turn it OFF.
                StateMask = enable ? PROCESS_POWER_THROTTLING_EXECUTION_SPEED_RELIABILITY : 0
            };

            uint structureSize = (uint)Marshal.SizeOf(throttlingState);
            bool powerResult = SetProcessInformation(hProcess, ProcessPowerThrottling, ref throttlingState, structureSize);

            // 2. Configure Process Priority Class
            // Windows drops a process to Idle priority when Efficiency Mode is turned on.
            uint priorityClass = enable ? IDLE_PRIORITY_CLASS : NORMAL_PRIORITY_CLASS;
            bool priorityResult = SetPriorityClass(hProcess, priorityClass);

            return powerResult && priorityResult;
        }
    }
}
