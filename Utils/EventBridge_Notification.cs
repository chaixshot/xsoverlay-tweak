using HarmonyLib;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace xsoverlay_tweak.Utils
{
    internal class EventBridge_Notification : EventBridge
    {
        private static Coroutine stopCoroutine;
        public static bool IsVisible = false;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void ListenNotificationPush(DeviceManager __instance)
        {
            // Listen to notification push
            CustomAPI.OnShowNotification += async (notify) =>
            {
                IsVisible = true;

                if (stopCoroutine != null)
                    Plugin.Instance.StopCoroutine(stopCoroutine);
                stopCoroutine = Plugin.Instance.StartCoroutine(NotificationTimer(notify.timeout));

                await Task.Delay(1);
                ShowNotification(notify);
            };
        }

        private static IEnumerator NotificationTimer(float timeout)
        {
            yield return new WaitForSecondsRealtime(timeout);
            IsVisible = false;
            stopCoroutine = null;
        }
    }
}
