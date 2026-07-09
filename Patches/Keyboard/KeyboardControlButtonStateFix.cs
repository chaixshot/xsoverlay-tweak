using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Keyboard
{
    internal class KeyboardControlButtonStateFix
    {
        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.EnableKeyboard))]
        [HarmonyPostfix]
        public static void WhenEnableKeyboard(Overlay_Manager __instance)
        {
            if (!IsEnable()) return;

            SetControlButtonColor(__instance.Keyboard, __instance.Keyboard_Overlay);
        }

        [HarmonyPatch(typeof(Overlay_Manager), "HandleKeyboardSummoning")]
        [HarmonyPostfix]
        public static void WhenKeyboardSummoned(Overlay_Manager __instance)
        {
            if (!IsEnable()) return;

            SetControlButtonColor(__instance.Keyboard, __instance.Keyboard_Overlay);
        }

        private static void SetControlButtonColor(GameObject keyboard, Unity_Overlay keyboard_Overlay)
        {
            if (keyboard.activeSelf)
            {
                { // Pin
                    Button component = keyboard.GetComponentInChildren<PinWindowButton>(includeInactive: true).gameObject.GetComponent<Button>();
                    ColorBlock colors = component.colors;
                    colors.normalColor = (keyboard_Overlay.isPinned ? UIThemeHandler.Instance.T_MidTone : UIThemeHandler.Instance.T_DarkTone);
                    component.colors = colors;
                }

                { // Lock
                    Button component = keyboard.GetComponentInChildren<LockKeyboardButton>(includeInactive: true).gameObject.GetComponent<Button>();
                    ColorBlock colors = component.colors;
                    colors.normalColor = (keyboard_Overlay.IsWindowInteractionLocked ? UIThemeHandler.Instance.T_MidTone : UIThemeHandler.Instance.T_DarkTone);
                    component.colors = colors;
                }
            }
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardControlButtonStateFix.Value;
        }
    }
}
