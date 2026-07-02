using HarmonyLib;
using System.Collections;
using UnityEngine;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class StickyKeyHaptic
    {
        [HarmonyPatch(typeof(KeyboardKey), "VirtualKeyboardEvent")]
        [HarmonyPostfix]
        public static void VirtualKeyboardEvent(KeyboardKey __instance, bool ___WaitingForDoubleTap)
        {
            if (!IsEnable()) return;

            if (__instance.IsDoubleTappable)
                if (!___WaitingForDoubleTap && __instance.KeyIsCurrentlyHoldLocked)
                    Plugin.Instance.StartCoroutine(StickyHaptic());
        }

        private static IEnumerator StickyHaptic()
        {
            yield return new WaitForSecondsRealtime(0.1f);

            AdvancedHaptics.Rumble((DesktopCursorManager.Instance.GetCurrentInputDevice().HapticDeviceName == Raycaster.HapticDevice.Left), 0.1f, 320f, 0.5f);
        }

        private static bool IsEnable()
        {
            return XConfig.StickyKeyHaptic.Value;
        }
    }
}
