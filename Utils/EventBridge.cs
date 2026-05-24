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
        private static Coroutine NotificationCoroutine;

        public static bool IsNotificationVisible = false;
        public static bool IsHoverAnyOverlay = false;

        public static readonly Action<DeviceManager> GetHMDRefreshRateDelegate = AccessTools.MethodDelegate<Action<DeviceManager>>(AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate"));
        public static readonly Func<Raycaster, RayCastResult?> GetDesktopCoordinateDelegate = AccessTools.MethodDelegate<Func<Raycaster, RayCastResult?>>(AccessTools.Method(typeof(Raycaster), "GetDesktopCoordinate"));

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

                GetHMDRefreshRateDelegate(__instance);
            };

            // Listen to hovering overlay change
            {
                XSOEventSystem.OnSwitchHoveringOverlay += (raycaster, overlay) =>
                {
                    IsHoverAnyOverlay = true;
                    GetHMDRefreshRateDelegate(__instance);
                };

                XSOEventSystem.OnTakeControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = true;
                    GetHMDRefreshRateDelegate(__instance);
                };

                XSOEventSystem.OnReleaseControlOfDesktopCursor += (raycaster) =>
                {
                    IsHoverAnyOverlay = false;
                    GetHMDRefreshRateDelegate(__instance);
                };
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
    }
}
