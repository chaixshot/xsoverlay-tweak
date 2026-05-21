using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Utils
{
    internal class EventBridge
    {
        private static Coroutine NotificationCoroutine;

        public static bool IsNotificationVisible = false;
        public static bool IsHoverAnyOverlay = false;

        private static readonly Action<DeviceManager> GetHMDRefreshRateDelegate = AccessTools.MethodDelegate<Action<DeviceManager>>(AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate"));

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void Start(DeviceManager __instance)
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

        private static IEnumerator NotificationTimer(float timeout)
        {
            yield return new WaitForSecondsRealtime(timeout);
            IsNotificationVisible = false;
            NotificationCoroutine = null;
        }
    }
}
