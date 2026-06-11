using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class HandleScrollingFix
    {
        private static float _horizontalTicks;
        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool FixScrollingSpeed(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref int ___ScrollClicksPerSecond, ref float ____tickAccumulator, ref Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;

            float baseScrollSpeed = XSettingsManager.Instance.Settings.ScrollSpeed;
            float scrollFactor = baseScrollSpeed / RefreshRate.HMDRefreshRate;
            float deadzone = 0.15f;

            // Read BOTH horizontal (x)and vertical (y) axes from the input device
            float scrollX = ___InputDevice.NormalizedScrollAxis.x;
            float scrollY = ___InputDevice.NormalizedScrollAxis.y;

            float absX = Mathf.Abs(scrollX);
            float absY = Mathf.Abs(scrollY);

            // If both axes are inside the deadzone, or click engine is broken, stop processing
            if ((absX <= deadzone && absY <= deadzone) || (float)___ScrollClicksPerSecond <= 0f)
            {
                return false;
            }

            if (__instance.HoveringOverlay.IsDesktopOrWindowCapture)
            {
                if (absX > deadzone) // Handle Horizontal Scrolling
                {
                    _horizontalTicks += absX * (float)___ScrollClicksPerSecond * scrollFactor;
                    int horizontalTicks = (int)_horizontalTicks;
                    if (horizontalTicks > 0)
                    {
                        _horizontalTicks -= horizontalTicks;
                        XInputManager.sim.Mouse.HorizontalScroll((scrollX > 0f) ? 1 : -1 * horizontalTicks);
                    }
                }

                if (absY > deadzone) // Handle Vertical Scrolling
                {
                    ____tickAccumulator += absY * (float)___ScrollClicksPerSecond * scrollFactor;
                    int verticalTicks = (int)____tickAccumulator;
                    if (verticalTicks > 0)
                    {
                        ____tickAccumulator -= verticalTicks;
                        MouseOperations.Scroll(((scrollY > 0f) ? 1 : (-1)) * verticalTicks, XInputManager.sim);
                    }
                }
            }
            else if (__instance.HoveringOverlay.IsPluginApplication)
            {
                // Vector mapping for embedded browser engine frames: 
                // Inverts Y for browser standard window scrolling coordinates, retains direct X
                float webScrollX = scrollX * scrollFactor;
                float webScrollY = 0f - (scrollY * scrollFactor);

                __instance.HoveringOverlay.WebViewHandler.WebView.Scroll(
                    new(webScrollX, webScrollY),
                    ___CursorUVNormalized
                );

            }

            EventBridge.HandleScrolling(___InputDevice.Scroll.axis, ___CursorUVNormalized);

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.HandleScrollingFix.Value;
        }
    }
}
