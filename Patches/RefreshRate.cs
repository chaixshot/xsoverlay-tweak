using HarmonyLib;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    internal class RefreshRate
    {
        private static float HMDRefreshRate = 90f;
        private static float LastGrabTime;
        private static float GrabbedDistance = 0f;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void Start(DeviceManager __instance)
        {
            if (__instance.HMDRefreshRate > 0)
                HMDRefreshRate = __instance.HMDRefreshRate;

            // Setting changed
            XConfig.EnableRefreshRate.SettingChanged += (sender, args) =>
            {
                AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);
            };

            XConfig.RefreshRate.SettingChanged += (sender, args) =>
            {
                AccessTools.Method(typeof(DeviceManager), "GetHMDRefreshRate").Invoke(__instance, null);
            };
        }

        [HarmonyPatch(typeof(DeviceManager), "GetHMDRefreshRate")]
        [HarmonyPrefix]
        public static bool GetHMDRefreshRate(DeviceManager __instance)
        {
            if (!IsEnable()) return true;

            XSTools.ExecuteOnMainThread(delegate
            {
                Application.targetFrameRate = XConfig.RefreshRate.Value.Equals(500) ? -1 : XConfig.RefreshRate.Value;
                Time.fixedDeltaTime = 1f / (float)XConfig.RefreshRate.Value;
            });

            return false;
        }

        // Fix Push/Pull speed
        [HarmonyPatch(typeof(Raycaster), "Grab")]
        [HarmonyPostfix]
        public static void Grab(ref float ___GrabbedDistance)
        {
            if (!IsEnable()) return;

            float currentTime = Time.unscaledTime;
            if (currentTime - LastGrabTime < (1f / HMDRefreshRate))
                ___GrabbedDistance = GrabbedDistance;
            else
            {
                GrabbedDistance = ___GrabbedDistance;
                LastGrabTime = currentTime;
            }
        }

        // Fix Scrolling speed
        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool HandleScrolling(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref int ___ScrollClicksPerSecond, ref float ____tickAccumulator, ref Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;

            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            float scrollSpeedMultiplier = XSettingsManager.Instance.Settings.ScrollSpeed * (currentFPS / HMDRefreshRate);

            float num = 0.2f;
            float y = ___InputDevice.NormalizedScrollAxis.y;
            float num2 = Mathf.Abs(y);
            if (num2 <= num || (float)___ScrollClicksPerSecond <= 0f)
            {
                return false;
            }

            if (__instance.HoveringOverlay.IsDesktopOrWindowCapture)
            {
                ____tickAccumulator += num2 * (float)___ScrollClicksPerSecond * scrollSpeedMultiplier * Time.unscaledDeltaTime;
                int num3 = (int)____tickAccumulator;
                if (num3 > 0)
                {
                    ____tickAccumulator -= num3;
                    MouseOperations.Scroll(((y > 0f) ? 1 : (-1)) * num3, XInputManager.sim);
                }
            }
            else if (__instance.HoveringOverlay.IsPluginApplication)
            {
                float num4 = y * scrollSpeedMultiplier * Time.unscaledDeltaTime;
                __instance.HoveringOverlay.WebViewHandler.WebView.Scroll(new Vector2(0f, 0f - num4), ___CursorUVNormalized);
            }

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.EnableRefreshRate.Value;
        }
    }
}
