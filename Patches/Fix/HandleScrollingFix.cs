using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Pointer;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class HandleScrollingFix
    {
        private static float ____horizontalTicks;

        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool FixScrollingSpeed(Raycaster __instance, MouseInputDevice ___InputDevice, int ___ScrollClicksPerSecond, ref float ____tickAccumulator, Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;

            // Read BOTH horizontal (x)and vertical (y) axes from the input device
            float scrollX = ___InputDevice.Scroll.axis.x;
            float scrollY = ___InputDevice.Scroll.axis.y;

            float absX = Mathf.Abs(scrollX);
            float absY = Mathf.Abs(scrollY);

            // If both axes are inside the deadzone, or click engine is broken, stop processing
            float deadzone = 0.01f;
            if ((absX <= deadzone && absY <= deadzone) || (float)___ScrollClicksPerSecond <= 0f)
                return false;

            // Two handed mode enable fix scrolling non current hand
            if (TwoHandedMode.IsEnable() && !EventBridge.IsActiveHand(__instance, true))
                return false;

            float baseScrollSpeed = XSettingsManager.Instance.Settings.ScrollSpeed;
            float scrollFactor = baseScrollSpeed / RefreshRate.HMDRefreshRate;

            if (__instance?.HoveringOverlay?.IsDesktopOrWindowCapture == true)
            {
                // Handle Horizontal Scrolling
                ____horizontalTicks += absX * (float)___ScrollClicksPerSecond * scrollFactor;
                int horizontalTicks = (int)____horizontalTicks;
                if (horizontalTicks > 0)
                {
                    ____horizontalTicks -= horizontalTicks;
                    XInputManager.sim.Mouse.HorizontalScroll(((scrollX > 0f) ? 1 : -1) * horizontalTicks);
                }

                // Handle Vertical Scrolling
                ____tickAccumulator += absY * (float)___ScrollClicksPerSecond * scrollFactor;
                int verticalTicks = (int)____tickAccumulator;
                if (verticalTicks > 0)
                {
                    ____tickAccumulator -= verticalTicks;
                    MouseOperations.Scroll((((scrollY > 0f) ? 1 : (-1))) * verticalTicks, XInputManager.sim);
                }
            }
            else if (__instance?.HoveringOverlay?.IsPluginApplication == true)
            {
                // Vector mapping for embedded browser engine frames: 
                // Inverts Y for browser standard window scrolling coordinates, retains direct X
                float webScrollX = scrollX * scrollFactor;
                float webScrollY = 0f - (scrollY * scrollFactor);

                // 0.00275 is the minimum value allowed for scrolling  WebView
                webScrollX = scrollX > 0f ? Mathf.Min(-0.00275f, webScrollX) : Mathf.Max(0.00275f, webScrollX);
                webScrollY = scrollY > 0f ? Mathf.Min(-0.00275f, webScrollY) : Mathf.Max(0.00275f, webScrollY);

                ____tickAccumulator += Mathf.Max(absX, absY) * (float)___ScrollClicksPerSecond * scrollFactor * 10;
                int verticalTicks = (int)____tickAccumulator;
                if (verticalTicks > 0)
                {
                    ____tickAccumulator -= verticalTicks;
                    __instance.HoveringOverlay.WebViewHandler.WebView.Scroll(
                        new(webScrollX, webScrollY),
                        ___CursorUVNormalized
                    );
                }
            }

            EventBridge.HandleScrolling(___InputDevice.Scroll.axis, ___CursorUVNormalized);

            return false;
        }

        public static bool IsEnable()
        {
            return XConfig.HandleScrollingFix.Value;
        }
    }
}
