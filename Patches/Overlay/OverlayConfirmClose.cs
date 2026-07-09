using HarmonyLib;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class OverlayConfirmClose
    {
        private static float lastClickTime = 0f;
        private static int clickCount = 0;

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.DeleteWindowGlobalOverlayMenu))]
        [HarmonyPrefix]
        public static bool TripleToDeleteWindowOverlay()
        {
            if (!IsEnable()) return true;

            float currentTime = Time.time;
            float timeSinceLastClick = currentTime - lastClickTime;

            if (timeSinceLastClick <= 0.3f) // If the click happened within the timeout window, progress the multi-click chain
                clickCount++;
            else
                clickCount = 1; // Too much time passed! Reset the chain count back to 1 for this fresh click

            lastClickTime = currentTime; // Always update the last click timestamp to track the next interval correctly

            if (clickCount >= 3)
            {
                clickCount = 0;
                lastClickTime = 0f;

                return true;
            }

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayConfirmClose.Value;
        }
    }
}
