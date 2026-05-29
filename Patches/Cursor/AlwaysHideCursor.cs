using HarmonyLib;
using System.Collections;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(WindowComponentManager))]
    internal class AlwaysHideCursor
    {
        [HarmonyPatch("OnSwitchHoveringOverlay"), HarmonyPatch("SetupWindow")]
        [HarmonyPostfix]
        public static void StartHide(WindowComponentManager __instance, ref bool ___WindowCanShowDesktopCursor)
        {
            if (!IsEnable()) return;

            ___WindowCanShowDesktopCursor = true; // SteamVR Dashboard Desktop make cursor reappear

            if (__instance?.WindowAPI?.window?.isDesktop == true) // Disable for Window Capture Mode, Cursor offsetting from Pointer
                Plugin.Instance.StartCoroutine(HideDelay(__instance));
        }

        private static IEnumerator HideDelay(WindowComponentManager __instance)
        {
            yield return new WaitForSecondsRealtime(0.05f);

            AccessTools.Field(typeof(WindowComponentManager), "WindowCanShowDesktopCursor").SetValue(__instance, false);
        }

        private static bool IsEnable()
        {
            return (XConfig.AlwaysHideCursor.Value || WindowsCursorPointer.IsEnable()) && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
    }
}