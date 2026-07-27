using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using WindowsInput.Native;
using XSOverlay;
using xsoverlay_tweak.Patches.Mouse;
using static xsoverlay_tweak.Patches.FocusedWindow.Utils;

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
                if (IsEnable() && !PhysicalMouseDetector.IsPhysicalMovement)
                    if (isShow)
                    {
                        IntPtr hwnd = GetForegroundWindow();

                        if (hwnd != IntPtr.Zero && IsWindowFullscreen(hwnd))
                        {
                            lastWindow = hwnd;
                            DoTask(hwnd);
                        }
                    }
                    else if (lastWindow != IntPtr.Zero) // Edit mode toggle off
                    {
                        switch (XConfig.FocusWindowFullscreen.Value)
                        {
                            case 1: // Close Task View
                                if (IsTaskViewOpen())
                                    XInputManager.sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                                SetForegroundWindow(lastWindow);

                                break;
                            case 2: // Close Start Menu
                                if (IsStartMenuOpen())
                                    XInputManager.sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                                SetForegroundWindow(lastWindow);

                                break;
                            case 3: // Restore from minimze
                                ShowWindow(lastWindow, SW_RESTORE);

                                break;
                        }
                        lastWindow = IntPtr.Zero;
                    }
            };
        }

        private static async void DoTask(IntPtr hwnd)
        {
            switch (XConfig.FocusWindowFullscreen.Value)
            {
                case 1: // Task View
                    await ShowWindowsTaskView();

                    if (IsWindowFullscreen(hwnd))
                        ShellStartMenu();

                    break;
                case 2: // Start menu
                    ShellStartMenu();

                    break;
                case 3: // Minimize
                    ShowWindow(hwnd, SW_MINIMIZE);

                    break;
            }
        }

        private static bool IsWindowFullscreen(IntPtr hWnd)
        {
            // Failsafe checks: Ensure we aren't checking a null handle, the desktop itself, or the Windows shell
            if (hWnd == IntPtr.Zero || hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
                return false;

            // Get the screen rectangle of the window
            if (!GetWindowRect(hWnd, out RECT windowRect))
                return false;

            // Find which monitor this window is currently occupying
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
                return false;

            // Get the bounds of that specific monitor
            MONITORINFO monitorInfo = new();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
                return (windowRect.Left <= monitorInfo.rcMonitor.Left &&
                        windowRect.Top <= monitorInfo.rcMonitor.Top &&
                        windowRect.Right >= monitorInfo.rcMonitor.Right &&
                        windowRect.Bottom >= monitorInfo.rcMonitor.Bottom);

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.FocusWindowFullscreen.Value != 0;
        }
    }
}