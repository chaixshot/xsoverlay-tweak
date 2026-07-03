using HarmonyLib;
using System;
using System.Runtime.InteropServices;
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
                    IntPtr hwnd = Utils.GetForegroundWindow();
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