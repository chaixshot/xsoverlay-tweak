using HarmonyLib;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Valve.VR; // Ensure the OpenVR namespace is included
using XSOverlay;

namespace xsoverlay_tweak.Patches.FocusedWindow
{
    internal class FullscreenMinimize
    {
        private static IntPtr lastWindow = IntPtr.Zero; // Keeps track of the window we minimized so we can bring it back

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForLayoutChanges()
        {
            XSOEventSystem.OnToggleLayoutMode += (isShow) =>
            {
                if (!IsEnable()) return;

                if (isShow)
                {
                    IntPtr hwnd = IntPtr.Zero;
                    uint vrPid = 0;

                    if (OpenVR.Applications != null)
                        vrPid = OpenVR.Applications.GetCurrentSceneProcessId();

                    if (vrPid != 0)
                        hwnd = GetWindowHandleFromPid((int)vrPid);
                    else
                        hwnd = Utils.GetForegroundWindow();

                    if (hwnd != IntPtr.Zero && IsWindowFullscreen(hwnd))
                    {
                        lastWindow = hwnd;
                        Utils.ShowWindow(hwnd, Utils.SW_MINIMIZE);
                    }
                }
                else if (lastWindow != IntPtr.Zero)
                {
                    Utils.ShowWindow(lastWindow, Utils.SW_RESTORE);
                    lastWindow = IntPtr.Zero;
                }
            };
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
            return XConfig.FullscreenMinimize.Value;
        }
    }
}