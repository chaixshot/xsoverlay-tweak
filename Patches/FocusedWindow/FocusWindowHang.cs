using Cysharp.Threading.Tasks;
using HarmonyLib;
using System;
using System.Threading;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.FocusedWindow
{
    internal class FocusWindowHang
    {
        private static IntPtr hookHandle;
        private static Utils.WinEventDelegate hookDelegate;
        private static CancellationTokenSource pollingCts; // Added to manage the background loop lifetime

        private static event Action<bool> OnFocusedWindowChanged;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForHangChanges()
        {
            XConfig.FocusWindowHang.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    SetupHook();
                else
                    ShutdownHook();
            };

            XSOEventSystem.OnToggleLayoutMode += async (IsShow) =>
            {
                if (IsEnable() && IsShow)
                    HandleHangingWindow();
            };

            OnFocusedWindowChanged += (isHanging) =>
            {
                if (isHanging && (EventBridge.IsHoverAnyDesktopOrWindowCapture || Overlay_Manager.Instance.editMode))
                    HandleHangingWindow();
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

                if (IsCurrentWindowHanging())
                    Utils.ShellStartMenu();
            }
            else if (mode == 2) // Start menu
                Utils.ShellStartMenu();
        }

        /// <summary>
        /// Centralized logic to strip input priority and invoke Task View if a window hangs
        /// </summary>
        private static void HandleHangingWindow()
        {
            bool confirmed = false;
            IntPtr hwnd = Utils.GetForegroundWindow();

            XSTools.ExecuteOnMainThread(async () =>
            {
                await UniTask.Delay(150); // Make sure the app is not just loading

                for (int i = 0; i < 3; i++)
                {
                    if (Utils.IsHungAppWindow(hwnd))
                    {
                        confirmed = true;
                        break;
                    }
                    await UniTask.Delay(300);
                }

                if (!confirmed) return;

                if (hwnd == Utils.GetForegroundWindow()) // Make sure the window is still the same
                    DoTask();
            });
        }

        /// <summary>
        /// Background polling loop to catch windows that hang *while* already focused.
        /// </summary>
        private static async UniTaskVoid StartHangPolling(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(1000, cancellationToken: token);

                if (EventBridge.IsHoverAnyDesktopOrWindowCapture || Overlay_Manager.Instance.editMode)
                    if (IsCurrentWindowHanging())
                    {
                        HandleHangingWindow();
                        await UniTask.Delay(3000, cancellationToken: token);
                    }
            }
        }

        private static bool IsCurrentWindowHanging()
        {
            IntPtr hwnd = Utils.GetForegroundWindow();
            return hwnd != IntPtr.Zero && Utils.IsHungAppWindow(hwnd);
        }

        private static void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero || idObject != 0) return;

            bool isHanging = Utils.IsHungAppWindow(hwnd);
            OnFocusedWindowChanged?.Invoke(isHanging);
        }

        private static void SetupHook()
        {
            if (hookHandle != IntPtr.Zero) return;

            // Start Hook
            hookDelegate = new Utils.WinEventDelegate(WinEventCallback);
            hookHandle = Utils.SetWinEventHook(
                Utils.EVENT_SYSTEM_FOREGROUND, Utils.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, hookDelegate, 0, 0, Utils.WINEVENT_OUTOFCONTEXT
            );

            // Start Polling Loop
            pollingCts = new CancellationTokenSource();
            StartHangPolling(pollingCts.Token).Forget();
        }

        private static void ShutdownHook()
        {
            // Stop Polling Loop
            if (pollingCts != null)
            {
                pollingCts.Cancel();
                pollingCts.Dispose();
                pollingCts = null;
            }

            // Stop Hook
            if (hookHandle == IntPtr.Zero) return;
            Utils.UnhookWinEvent(hookHandle);
            hookHandle = IntPtr.Zero;
            hookDelegate = null;
        }

        private static bool IsEnable()
        {
            return XConfig.FocusWindowHang.Value != 0;
        }
    }
}