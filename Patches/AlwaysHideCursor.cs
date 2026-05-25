using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace xsoverlay_tweak.Patches
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

            if (__instance.WindowAPI != null && __instance.WindowAPI.window.isDesktop) // Disable for Window Capture Mode, Cursor offsetting from Pointer
                Plugin.Instance.StartCoroutine(HideDelay(__instance));
        }

        private static IEnumerator HideDelay(WindowComponentManager __instance)
        {
            yield return new WaitForSecondsRealtime(0.05f);

            AccessTools.Field(typeof(WindowComponentManager), "WindowCanShowDesktopCursor").SetValue(__instance, false);
        }

        private static bool IsEnable()
        {
            return XConfig.AlwaysHideCursor.Value || XConfig.WindowsCursorPointer.Value;
        }
    }
}