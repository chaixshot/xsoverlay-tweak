using Cysharp.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using WindowsInput.Native;
using XSOverlay;

namespace xsoverlay_tweak.Patches.FocusedWindow
{
    internal static class Utils
    {
        #region Native Win32 API Imports

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsHungAppWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr voidProcessId);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
        #endregion

        #region Constants and Delegates

        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        internal const int SW_MINIMIZE = 6;
        internal const int SW_RESTORE = 9;

        internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        internal const uint TOKEN_QUERY = 0x0008;
        internal const int TokenElevationType = 18;
        internal const int TokenElevationTypeFull = 2;

        internal const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        internal const uint WM_SYSCOMMAND = 0x0112;
        internal const int SC_TASKLIST = 0xF130;
        #endregion

        /// <summary>
        /// Safely forces background context execution and flashes Windows Task View.
        /// </summary>
        internal static async UniTask ShowWindowsTaskView()
        {
            static void taskViewCMD()
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c start shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }

            try
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);

                // Focus Taskbar & Press Win + Tab
                if (taskbarHandle != IntPtr.Zero)
                {
                    await UniTask.Delay(200); // Wait for active window from taskbar tray icon
                    SetForegroundWindow(taskbarHandle);
                    await UniTask.Delay(200); // Wait for Windows to update focus and drop any elevated window security locks (UIPI)

                    try // Press Win + Tab
                    {
                        XInputManager.sim.Keyboard.KeyDown(VirtualKeyCode.LWIN);
                        XInputManager.sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    }
                    catch (Exception)
                    {
                        taskViewCMD();
                    }
                    finally // Releases Win + Tab safely under any execution failure
                    {
                        XInputManager.sim.Keyboard.KeyUp(VirtualKeyCode.LWIN);
                    }
                }
                else // Fallback - Open Start Menu directly via SysCommand
                {
                    IntPtr progmanHandle = FindWindow("Progman", null);
                    IntPtr targetShell = progmanHandle != IntPtr.Zero ? progmanHandle : taskbarHandle;

                    SendMessage(targetShell, WM_SYSCOMMAND, (IntPtr)SC_TASKLIST, IntPtr.Zero); // SendMessage ignores UIPI blocks completely
                }
            }
            catch (Exception) // Fallback - If the entire explorer UI is completely dead/restarting
            {
                taskViewCMD();
            }
        }
    }
}