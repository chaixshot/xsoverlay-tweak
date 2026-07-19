using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.FocusedWindow
{
    internal class FocusWindowElevated
    {
        private static IntPtr hookHandle;
        private static Utils.WinEventDelegate hookDelegate;

        private static event Action<bool> OnFocusedWindowChanged;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForConfigChange()
        {
            if (IsXSOverlayElevated()) return;

            XConfig.FocusWindowElevated.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    SetupHook();
                else
                    ShutdownHook();
            };

            // Toggle edit mode and focused window is elevated
            XSOEventSystem.OnToggleLayoutMode += async (IsShow) =>
            {
                if (IsEnable() && IsShow)
                {
                    if (IsCurrentWindowElevated())
                        DoTask();
                }
            };

            // Hovering desktop capture and new focus window is elevated
            OnFocusedWindowChanged += async (isElevated) =>
            {
                if (isElevated && (EventBridge.IsHoverAnyDesktopOrWindowCapture() || Overlay_Manager.Instance.editMode))
                    DoTask();
            };

            if (IsEnable())
                SetupHook();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ShutdownHook();
        }

        private static async void DoTask()
        {
            int mode = XConfig.FocusWindowElevated.Value;

            if (mode == 1) // Task View
            {
                await Utils.ShowWindowsTaskView();

                if (IsCurrentWindowElevated())
                    Utils.ShellStartMenu();
            }
            else if (mode == 2) // Start menu
                Utils.ShellStartMenu();
        }

        /// <summary>
        /// Checks if the currently executing application/mod has Administrator privileges.
        /// </summary>
        private static bool IsXSOverlayElevated()
        {
            IntPtr myHwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            if (myHwnd != IntPtr.Zero)
                return IsWindowElevated(myHwnd);

            return false;
        }

        private static bool IsCurrentWindowElevated()
        {
            IntPtr hwnd = Utils.GetForegroundWindow();
            return hwnd != IntPtr.Zero && IsWindowElevated(hwnd);
        }

        private static void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero || idObject != 0) return; // 0 == OBJID_WINDOW

            bool isElevated = IsWindowElevated(hwnd);
            OnFocusedWindowChanged?.Invoke(isElevated);
        }

        private static bool IsWindowElevated(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            // Allocate memory for the Process ID because of the specific P/Invoke signature provided
            IntPtr pidPtr = Marshal.AllocHGlobal(sizeof(uint));
            IntPtr hProcess = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            IntPtr pElevationType = IntPtr.Zero;

            try
            {
                // Get the Process ID from the Window Handle
                Utils.GetWindowThreadProcessId(hWnd, pidPtr);
                uint processId = (uint)Marshal.ReadInt32(pidPtr);
                if (processId == 0) return false;

                // Open the process with limited query rights
                hProcess = Utils.OpenProcess(Utils.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                if (hProcess == IntPtr.Zero) return false;

                // Open the process's access token
                if (!Utils.OpenProcessToken(hProcess, Utils.TOKEN_QUERY, out hToken)) return true;

                // Query the TokenElevationType
                pElevationType = Marshal.AllocHGlobal(sizeof(int));
                if (Utils.GetTokenInformation(hToken, Utils.TokenElevationType, pElevationType, sizeof(int), out uint returnLength))
                {
                    int elevationType = Marshal.ReadInt32(pElevationType);

                    // TokenElevationTypeFull (2) means the process is running elevated (as Admin)
                    return elevationType == Utils.TokenElevationTypeFull;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error checking window elevation: {ex.Message}");
            }
            finally
            {
                // Clean up all allocated unmanaged memory and handles
                if (pidPtr != IntPtr.Zero) Marshal.FreeHGlobal(pidPtr);
                if (pElevationType != IntPtr.Zero) Marshal.FreeHGlobal(pElevationType);
                if (hToken != IntPtr.Zero) Utils.CloseHandle(hToken);
                if (hProcess != IntPtr.Zero) Utils.CloseHandle(hProcess);
            }

            return false;
        }

        private static void SetupHook()
        {
            if (hookHandle != IntPtr.Zero) return;

            hookDelegate = new Utils.WinEventDelegate(WinEventCallback);
            hookHandle = Utils.SetWinEventHook(
                Utils.EVENT_SYSTEM_FOREGROUND, Utils.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, hookDelegate, 0, 0, Utils.WINEVENT_OUTOFCONTEXT
            );
        }

        private static void ShutdownHook()
        {
            if (hookHandle == IntPtr.Zero) return;

            Utils.UnhookWinEvent(hookHandle);
            hookHandle = IntPtr.Zero;
            hookDelegate = null;
        }

        private static bool IsEnable()
        {
            return XConfig.FocusWindowElevated.Value != 0;
        }
    }
}