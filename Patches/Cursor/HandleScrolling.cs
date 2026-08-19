using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Pointer;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class HandleScrolling
    {
        private static float ____horizontalTicks;
        private static readonly float scrollClicksPerSecond = 25f;

        [HarmonyPatch(typeof(Raycaster), "HandleScrolling")]
        [HarmonyPrefix]
        public static bool FixScrollingSpeed(Raycaster __instance, MouseInputDevice ___InputDevice, Vector2 ___CursorUVNormalized)
        {
            if (!IsEnable()) return true;

            float scrollX = ___InputDevice.Scroll.axis.x; // horizontal (x) from the input device
            float absX = Mathf.Abs(scrollX);

            // If X axe are inside the deadzone, or click engine is broken, stop processing
            float deadzone = 0.01f;
            if ((absX <= deadzone) || (float)scrollClicksPerSecond <= 0f)
                return true;

            // Two handed mode enable fix scrolling non current hand
            if (TwoHandedMode.IsEnable() && !EventBridge.IsActiveHand(__instance, true))
                return false;

            float baseScrollSpeed = XSettingsManager.Instance.Settings.ScrollSpeed;
            float scrollFactor = baseScrollSpeed / RefreshRate.HMDRefreshRate;

            if (__instance?.HoveringOverlay?.IsDesktopOrWindowCapture == true)
            {
                // Handle Horizontal Scrolling
                ____horizontalTicks += absX * (float)scrollClicksPerSecond * scrollFactor;
                int horizontalTicks = (int)____horizontalTicks;
                if (horizontalTicks > 0)
                {
                    ____horizontalTicks -= horizontalTicks;
                    InputManager.sim.Mouse.HorizontalScroll(((scrollX > 0f) ? 1 : -1) * horizontalTicks);
                }
            }

            EventBridge.HandleScrolling(___InputDevice.Scroll.axis, ___CursorUVNormalized);

            return true;
        }

        public static bool IsEnable()
        {
            return XConfig.HandleScrolling.Value;
        }
    }
}
