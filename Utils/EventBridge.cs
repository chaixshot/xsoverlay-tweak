using HarmonyLib;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using XSOverlay.WebApp;

namespace xsoverlay_tweak.Utils
{
    internal class EventBridge
    {
        public static readonly float OneCentimetre = 0.01f;
        public static readonly float OneDegree = 1.0f;

        protected static bool isKeyboardSpawning = false;

        public static event Action InputMethodChanged;
        public static event Action<Raycaster, Unity_Overlay> OnSwitchHoveringOverlay;
        public static event Action<CustomAPI.XSONotificationObject> OnShowNotification;
        public static event Action<Raycaster> OnTakeControlOfDesktopCursor;
        public static event Action<Raycaster> OnReleaseControlOfDesktopCursor;
        public static event Action<Vector2, Vector2> OnHandleScrolling;

        internal class Ref_DeviceManager
        {
            public static readonly Action<DeviceManager> GetHMDRefreshRate = AccessTools.MethodDelegate<Action<DeviceManager>>(AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate"));
        }

        internal class Ref_Raycaster
        {
            public delegate bool TryGetDesktopCoordinateDelegate(Raycaster instance, out Vector2 desktopCoordinate);

            public static readonly Action<Raycaster> TakeControlOverCursorIfNotInControl = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));
            public static readonly TryGetDesktopCoordinateDelegate TryGetDesktopCoordinate = (TryGetDesktopCoordinateDelegate)AccessTools.Method(typeof(Raycaster), "TryGetDesktopCoordinate").CreateDelegate(typeof(TryGetDesktopCoordinateDelegate));

            // NativeHoverState
            protected static readonly Type NativeHoverState = typeof(Raycaster).GetNestedType("NativeHoverState", AccessTools.all);
            public static readonly IDictionary NativeHoverStates = (IDictionary)AccessTools.Field(typeof(Raycaster), "NativeHoverStates").GetValue(null);
            public static readonly AccessTools.FieldRef<object, Raycaster> NativeHoverState_Owner = AccessTools.FieldRefAccess<Raycaster>(NativeHoverState, "Owner");
        }

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void InitializeEvents(DeviceManager __instance)
        {
            // Listen to hovering overlay change
            XSOEventSystem.OnSwitchHoveringOverlay += async (raycaster, overlay) =>
            {
                await Task.Delay(1);
                OnSwitchHoveringOverlay?.Invoke(raycaster, overlay);
            };

            // Listen to active raycaster change
            XSOEventSystem.OnTakeControlOfDesktopCursor += async (raycaster) =>
            {
                await Task.Delay(1);
                OnTakeControlOfDesktopCursor?.Invoke(raycaster);
            };

            // Listen to active raycaster change
            XSOEventSystem.OnReleaseControlOfDesktopCursor += async (raycaster) =>
            {
                await Task.Delay(1);
                OnReleaseControlOfDesktopCursor?.Invoke(raycaster);
            };
        }

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPostfix]
        public static void SetSetting(string name, string value, string value1, bool sendAnalytics = true)
        {
            if (name.Equals("InputMethod"))
                InputMethodChanged?.Invoke();
        }

        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.MoveToEdgeOfWindowAndInheritRotation))]
        [HarmonyPrefix]
        public static bool BlockKeyboardSpawnAboveWrist(Unity_Overlay Overlay)
        {
            if (isKeyboardSpawning && Overlay.overlayName == "keyboard")
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

        protected static void HandleScrolling(Vector2 ScrollAxis, Vector2 normalizedPoint) => OnHandleScrolling?.Invoke(ScrollAxis, normalizedPoint);
        protected static void ShowNotification(CustomAPI.XSONotificationObject notify) => OnShowNotification?.Invoke(notify);

        /// <summary>
        /// Toogle keyboard by using API command to support OSC Keyboard mod
        /// </summary>
        /// <param name="isShow"></param>
        public static void ToggleKeyboardExecuteAPI(bool isShow)
        {
            Overlay_Manager overlay_Manager = Overlay_Manager.Instance;
            Unity_Overlay keyboard = overlay_Manager.Keyboard_Overlay;
            bool isActive = overlay_Manager.Keyboard.gameObject.activeSelf;

            isKeyboardSpawning = true;

            if (isShow)
            {
                if (!isActive) // Show keyboard if unsummoned
                    ServerClientBridge.Instance.Api.Commands["Keyboard"]("", "", "");
            }
            else if (isActive) // Hide keyboard if summoned
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
                isKeyboardSpawning = false;
            });
        }

        public static bool IsNotificationVisible() => EventBridge_Notification.IsVisible;

        //## Raycaster
        public static bool IsHoverAnyOverlay() => EventBridge_Raycaster.IsHoverAnyOverlay;
        public static bool IsHoverAnyDesktopOrWindowCapture() => EventBridge_Raycaster.IsHoverAnyDesktopOrWindowCapture;
        public static bool IsHoverAnyDesktopCapture() => EventBridge_Raycaster.IsHoverAnyDesktopCapture;
        public static bool IsHoverAnyWindowCapture() => EventBridge_Raycaster.IsHoverAnyWindowCapture;
        public static bool IsHoverAnyWebView() => EventBridge_Raycaster.IsHoverAnyWebView;
        public static bool IsActiveHand(Raycaster raycaster, bool skipTwoHanded = false) => EventBridge_Raycaster.IsActiveHand(raycaster, skipTwoHanded);
        public static bool IsActiveHandForWebView(Raycaster raycaster) => EventBridge_Raycaster.IsActiveHandForWebView(raycaster);
        public static bool IsRaycasterHand(Raycaster raycaster) => EventBridge_Raycaster.IsRaycasterHand(raycaster);
        public static Raycaster GetActiveRaycaster() => EventBridge_Raycaster.ActiveRaycaster;
        public static Raycaster GetActiveWebViewRaycaster() => EventBridge_Raycaster.ActiveWebViewRaycaster;
        public static Raycaster GetActiveDesktopRaycaster() => EventBridge_Raycaster.ActiveDesktopRaycaster;

        public static Unity_Overlay GetCurrentHoveringOverlay() => EventBridge_Raycaster.CurrentHoveringOverlay;
    }
}
