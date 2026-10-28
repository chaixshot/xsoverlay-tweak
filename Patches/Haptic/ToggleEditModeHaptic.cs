using HarmonyLib;
using System.Collections;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class ToggleEditModeHaptic
    {
        private static Coroutine HapticCoroutine;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            XSOEventSystem.OnToggleLayoutMode += (IsShow) =>
            {
                if (!IsEnable()) return;

                if (HapticCoroutine != null)
                    Plugin.Instance.StopCoroutine(HapticCoroutine);

                if (IsShow)
                    HapticCoroutine = Plugin.Instance.StartCoroutine(PlayShowHaptic());
                else
                    HapticCoroutine = Plugin.Instance.StartCoroutine(PlayHideHaptic());
            };
        }

        private static IEnumerator PlayShowHaptic()
        {
            float strength = 0.01f;
            var wait = new WaitForSecondsRealtime(0.01f);

            while (strength < 0.4f)
            {
                AdvancedHaptics.Rumble(true, 0f, 320f, strength);
                AdvancedHaptics.Rumble(false, 0f, 320f, strength);

                yield return wait;

                strength += 0.035f;
            }

            AdvancedHaptics.Rumble(true, 0f, 320f, 1f);
            AdvancedHaptics.Rumble(false, 0f, 320f, 1f);

            HapticCoroutine = null;
        }

        private static IEnumerator PlayHideHaptic()
        {
            var wait = new WaitForSecondsRealtime(0.1f);

            AdvancedHaptics.Rumble(true, 0f, 320f, 1f);
            AdvancedHaptics.Rumble(false, 0f, 320f, 1f);

            yield return wait;

            AdvancedHaptics.Rumble(true, 0f, 320f, 0.5f);
            AdvancedHaptics.Rumble(false, 0f, 320f, 0.5f);

            yield return wait;

            AdvancedHaptics.Rumble(true, 0f, 320f, 0.3f);
            AdvancedHaptics.Rumble(false, 0f, 320f, 0.3f);

            yield return wait;

            AdvancedHaptics.Rumble(true, 0f, 320f, 0.1f);
            AdvancedHaptics.Rumble(false, 0f, 320f, 0.1f);

            HapticCoroutine = null;
        }

        private static bool IsEnable()
        {
            return XConfig.ToggleEditModeHaptic.Value;
        }
    }
}
