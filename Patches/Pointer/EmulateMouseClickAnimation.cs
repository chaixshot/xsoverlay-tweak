using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class EmulateMouseClickAnimation
    {
        [HarmonyPatch("SendCapturedPressClick"), HarmonyPatch("SendCapturedPressDown")]
        [HarmonyPostfix]
        public static void ShowAnimationWhenClicksForDesktopWindows(Raycaster __instance, GameObject ___VisualCursorElementClickAnimation, Unity_Overlay ___VisualCursorElementClickAnimationOverlay, bool __result)
        {
            if (!IsEnable()) return;
            if (!__result) return;
            if (EventBridge.IsOverlayWebView(__instance.HoveringOverlay)) return;

            ___VisualCursorElementClickAnimation.transform.rotation = Quaternion.LookRotation(___VisualCursorElementClickAnimation.transform.position - Overlay_Manager.Instance.head.position);
            ___VisualCursorElementClickAnimationOverlay.gameObject.SetActive(value: true);
        }

        private static bool IsEnable()
        {
            return XConfig.EmulateMouseClickAnimation.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
    }
}
