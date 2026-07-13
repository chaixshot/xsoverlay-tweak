using HarmonyLib;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Valve.VR; // Ensure the OpenVR namespace is included
using XSOverlay;

namespace xsoverlay_tweak.Patches.FocusedWindow
{
    internal class FocusWindowFullscreen
    {
        private static IntPtr lastWindow = IntPtr.Zero; // Keeps track of the window we minimized so we can bring it back

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForLayoutChanges()
        {
            XSOEventSystem.OnToggleLayoutMode += async (isShow) =>
            {
                if (!IsEnable()) return;

                if (isShow)
                {
                    IntPtr hwnd = IntPtr.Zero;
                    uint vrPid = 0;

                    if (OpenVR.Applications != null)
                        vrPid = OpenVR.Applications.GetCurrentSceneProcessId();

                    if (vrPid != 0) // Get from OpnVR
                        hwnd = GetWindowHandleFromPid((int)vrPid);
                    else // Get from focused window
                        hwnd = Utils.GetForegroundWindow();

                    if (hwnd != IntPtr.Zero && IsWindowFullscreen(hwnd))
                    {
                        lastWindow = hwnd;
                        DoTask(hwnd);
                    }
                }
                else if (lastWindow != IntPtr.Zero) // Edit mode toggle off
                {
                    if (XConfig.FocusWindowFullscreen.Value == 1 || XConfig.FocusWindowFullscreen.Value == 2) // Focus back
                        Utils.SetForegroundWindow(lastWindow);
                    else if (XConfig.FocusWindowFullscreen.Value == 3) // Resore from minimze
                    {
                        Utils.ShowWindow(lastWindow, Utils.SW_RESTORE);
                        lastWindow = IntPtr.Zero;
                    }
                }
            };
        }

        private static async void DoTask(IntPtr hwnd)
        {
            int mode = XConfig.FocusWindowFullscreen.Value;

            if (mode == 1) // Task View
            {
                await Utils.ShowWindowsTaskView();

                if (IsWindowFullscreen(hwnd))
                    Utils.ShellStartMenu();
            }
            else if (mode == 2) // Start menu
                Utils.ShellStartMenu();
            else if (mode == 3) // Minimize
                Utils.ShowWindow(hwnd, Utils.SW_MINIMIZE);
        }

        /// <summary>
        /// Looks up the main window handle associated with the active VR game PID.
        /// </summary>
        private static IntPtr GetWindowHandleFromPid(int pid)
        {
            try
            {
                using Process proc = Process.GetProcessById(pid);
                return proc.MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static bool IsWindowFullscreen(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !Utils.GetWindowRect(hWnd, out Utils.RECT windowRect))
                return false;

            IntPtr hMonitor = Utils.MonitorFromWindow(hWnd, Utils.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
                return false;

            Utils.MONITORINFO monitorInfo = new() { cbSize = Marshal.SizeOf(typeof(Utils.MONITORINFO)) };
            if (!Utils.GetMonitorInfo(hMonitor, ref monitorInfo))
                return false;

            return windowRect.Left <= monitorInfo.rcMonitor.Left &&
                   windowRect.Top <= monitorInfo.rcMonitor.Top &&
                   windowRect.Right >= monitorInfo.rcMonitor.Right &&
                   windowRect.Bottom >= monitorInfo.rcMonitor.Bottom;
        }

        private static bool IsEnable()
        {
            return XConfig.FocusWindowFullscreen.Value != 0;
        }
    }
}