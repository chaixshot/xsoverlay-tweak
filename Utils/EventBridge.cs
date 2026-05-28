using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
using xsoverlay_tweak.Patches;

namespace xsoverlay_tweak.Utils
{
    internal class EventBridge
    {
        public static bool IsHoverAnyOverlay = false;

        private static Coroutine NotificationCoroutine;
        public static bool IsNotificationVisible = false;

        private static Coroutine CurrentHoveringOverlayCoroutine;
        public static Unity_Overlay CurrentHoveringOverlay;

        public static readonly Action<Raycaster> TakeControlOverCursorIfNotInControl = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));
        public static readonly Action<DeviceManager> GetHMDRefreshRate = AccessTools.MethodDelegate<Action<DeviceManager>>(AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate"));
        public static readonly Func<Raycaster, RayCastResult?> GetDesktopCoordinate = AccessTools.MethodDelegate<Func<Raycaster, RayCastResult?>>(AccessTools.Method(typeof(Raycaster), "GetDesktopCoordinate"));

        public static event Action InputMethodChanged;

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

                GetHMDRefreshRate(__instance);
            };

            // Listen to hovering overlay change
            {
                XSOEventSystem.OnSwitchHoveringOverlay += (raycaster, overlay) =>
                {
                    IsHoverAnyOverlay = true;
                    CurrentHoveringOverlay = overlay;
                    GetHMDRefreshRate(__instance);
                };

                XSOEventSystem.OnTakeControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = true;
                    GetHMDRefreshRate(__instance);

                    if (CurrentHoveringOverlayCoroutine != null)
                        Plugin.Instance.StopCoroutine(CurrentHoveringOverlayCoroutine);
                };

                XSOEventSystem.OnReleaseControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = false;
                    GetHMDRefreshRate(__instance);

                    CurrentHoveringOverlayCoroutine = Plugin.Instance.StartCoroutine(ClearCurrentHoveringOverlayTimer());
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
            return overlay != null && overlay.overlayName.Equals("keyboard");
        }

        private static IEnumerator ClearCurrentHoveringOverlayTimer()
        {
            yield return new WaitForSecondsRealtime(1);

            CurrentHoveringOverlay = null;
        }
    }
}
