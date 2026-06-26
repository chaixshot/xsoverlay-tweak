using HarmonyLib;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
using XSOverlay.WebApp;
using xsoverlay_tweak.Patches.Cursor;

namespace xsoverlay_tweak.Utils
{
    internal class EventBridge
    {
        public static readonly float OneCentimetre = 0.01f;
        public static readonly float OneDegree = 1.0f;

        public static bool IsHoverAnyOverlay = false;
        public static bool IsHoverAnyDesktopOrWindowCapture = false;
        public static bool IsHoverAnyDesktopCapture = false;
        public static bool IsHoverAnyWindowCapture = false;

        public static bool IsKeyboardSpawing = false;

        private static Coroutine NotificationCoroutine;
        public static bool IsNotificationVisible = false;

        private static Coroutine CurrentHoveringOverlayCoroutine;
        public static Unity_Overlay CurrentHoveringOverlay;

        public static event Action InputMethodChanged;
        public static event Action<Raycaster, Unity_Overlay> OnSwitchHoveringOverlay;
        public static event Action<Raycaster> OnTakeControlOfDesktopCursor;
        public static event Action<Raycaster> OnReleaseControlOfDesktopCursor;
        public static event Action<Vector2, Vector2> OnHandleScrolling;

        internal class Ref_DeviceManager
        {
            public static readonly Action<DeviceManager> GetHMDRefreshRate = AccessTools.MethodDelegate<Action<DeviceManager>>(AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate"));
        }

        internal class Ref_Raycaster
        {
            public static readonly Action<Raycaster> TakeControlOverCursorIfNotInControl = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));
            public static readonly Func<Raycaster, RayCastResult?> GetDesktopCoordinate = AccessTools.MethodDelegate<Func<Raycaster, RayCastResult?>>(AccessTools.Method(typeof(Raycaster), "GetDesktopCoordinate"));
            public static readonly AccessTools.FieldRef<Raycaster, float> InterpolationSpeed = AccessTools.FieldRefAccess<Raycaster, float>("InterpolationSpeed");
            public static readonly AccessTools.FieldRef<Raycaster, float> InterpolationDistance = AccessTools.FieldRefAccess<Raycaster, float>("InterpolationDistance");
        }

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void InitializeEvents(DeviceManager __instance)
        {
            // Listen to notification push
            XSOEventSystem.OnQueueNotification += (notify) =>
            {
                IsNotificationVisible = true;

                if (NotificationCoroutine != null)
                    Plugin.Instance.StopCoroutine(NotificationCoroutine);
                NotificationCoroutine = Plugin.Instance.StartCoroutine(NotificationTimer(notify.timeout));

                Ref_DeviceManager.GetHMDRefreshRate(__instance);
            };

            // Listen to hovering overlay change
            {
                XSOEventSystem.OnSwitchHoveringOverlay += (raycaster, overlay) =>
                {
                    IsHoverAnyOverlay = true;
                    if (overlay?.IsDesktopOrWindowCapture == true)
                        IsHoverAnyDesktopOrWindowCapture = true;
                    if (overlay?.IsDesktopCapture == true)
                        IsHoverAnyDesktopCapture = true;
                    if (overlay?.IsWindowCapture == true)
                        IsHoverAnyWindowCapture = true;

                    if (IsActiveHand(raycaster))
                        CurrentHoveringOverlay = overlay;

                    Ref_DeviceManager.GetHMDRefreshRate(__instance);

                    OnSwitchHoveringOverlay?.Invoke(raycaster, overlay);
                };

                XSOEventSystem.OnTakeControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = true;
                    Ref_DeviceManager.GetHMDRefreshRate(__instance);

                    if (IsActiveHand(raycaster))
                        CurrentHoveringOverlay = raycaster.HoveringOverlay;

                    if (CurrentHoveringOverlayCoroutine != null)
                        Plugin.Instance.StopCoroutine(CurrentHoveringOverlayCoroutine);

                    OnTakeControlOfDesktopCursor?.Invoke(raycaster);
                };

                XSOEventSystem.OnReleaseControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = false;
                    IsHoverAnyDesktopOrWindowCapture = false;
                    IsHoverAnyDesktopCapture = false;
                    IsHoverAnyWindowCapture = false;
                    Ref_DeviceManager.GetHMDRefreshRate(__instance);

                    CurrentHoveringOverlayCoroutine = Plugin.Instance.StartCoroutine(ClearCurrentHoveringOverlayTimer());

                    OnReleaseControlOfDesktopCursor?.Invoke(raycaster);
                };
            }
        }

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPostfix]
        public static void SetSetting(string name, string value, string value1, bool sendAnalytics = true)
        {
            switch (name)
            {
                case "InputMethod":
                    InputMethodChanged?.Invoke();

                    break;
            }
        }

        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.MoveToEdgeOfWindowAndInheritRotation))]
        [HarmonyPrefix]
        public static bool BlockKeyboardSpawnAboveWrist(Unity_Overlay Overlay)
        {
            if (IsKeyboardSpawing && Overlay.overlayName == "keyboard")
                return false;

            return true;
        }

        public static IEnumerator NotificationTimer(float timeout)
        {
            yield return new WaitForSecondsRealtime(timeout);
            IsNotificationVisible = false;
            NotificationCoroutine = null;
        }

        public static bool IsActiveHand(Raycaster __instance)
        {
            if (PhysicalMouseDetector.IsPhysicalMovement)
                return false;
            else if (DesktopCursorManager.Instance.GetCurrentInputDevice() != __instance)
                return false;

            return true;
        }

        public static bool IsOverlayWebView(Unity_Overlay overlay)
        {
            if (overlay == null)
                return false;

            string overlayName = overlay?.overlayName ?? "";
            return overlay.WebViewHandler != null && overlay.IsPluginApplication && !overlay.IsDesktopOrWindowCapture && !overlayName.Equals("wrist") && !overlayName.Equals("notification");
        }

        public static bool IsOverlayKeyboard(Unity_Overlay overlay)
        {
            return overlay?.overlayName == "keyboard";
        }

        public static void HandleScrolling(Vector2 ScrollAxis, Vector2 normalizedPoint)
        {
            OnHandleScrolling?.Invoke(ScrollAxis, normalizedPoint);
        }

        /// <summary>
        /// Toogle keyboard by using API command to support OSC Keyboard mod
        /// </summary>
        /// <param name="isShow"></param>
        public static void ExecuteApiToggleKeyboard(bool isShow)
        {
            Overlay_Manager overlay_Manager = Overlay_Manager.Instance;
            Unity_Overlay keyboard = overlay_Manager.Keyboard_Overlay;
            KeyboardGlobalManager keyboardManager = (KeyboardGlobalManager)AccessTools.Field(typeof(Overlay_Manager), "keyboardManager").GetValue(overlay_Manager);

            IsKeyboardSpawing = true;

            if (isShow)
            {
                if (!overlay_Manager.Keyboard.activeSelf || keyboardManager?.HasKeyboardBeenOpened == false) // Show keyboard if unsummoned
                    ServerClientBridge.Instance.Api.Commands["Keyboard"]("", "", "");
            }
            else if (overlay_Manager.Keyboard.activeSelf && keyboardManager?.HasKeyboardBeenOpened == true) // Hide keyboard if summoned
            {
                if (keyboard.isPinned) // Pinned keyboard can't unsummon
                {
                    overlay_Manager.PinKeyboard();
                    overlay_Manager.PinWindowSpecificWindow(keyboard);
                }

                ServerClientBridge.Instance.Api.Commands["Keyboard"]("", "", "");
            }

            Task.Run(async () =>
            {
                await Task.Delay(200);
                IsKeyboardSpawing = false;
            });
        }

        public static bool IsRaycasterHand(Raycaster raycaster)
        {
            return raycaster.HapticDeviceName != Raycaster.HapticDevice.None;
        }

        private static IEnumerator ClearCurrentHoveringOverlayTimer()
        {
            yield return new WaitForSecondsRealtime(1);

            CurrentHoveringOverlay = null;
        }
    }
}
