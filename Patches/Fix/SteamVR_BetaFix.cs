using HarmonyLib;
using System;
using UnityEngine;
using Valve.VR;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class SteamVR_BetaFix
    {
        private static readonly Version SteamVR_TargetVersion = new(2, 16);
        private static bool IsOverlayClipping = false;

        /// <summary>
        /// Push Pointer slightly closer to the player's face than Hover Overlay
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="___VisualCursorElement"></param>
        /// <param name="___VisualCursorElementClickAnimation"></param>
        [HarmonyPatch(typeof(Raycaster), "SetVisualCursorTransform")]
        [HarmonyPostfix]
        public static void FixPointerClipping(
            Raycaster __instance,
            ref GameObject ___VisualCursorElement)
        {
            if (!IsEnable() || !IsOverlayClipping || !EventBridge.IsRaycasterHand(__instance)) return;

            ___VisualCursorElement.transform.position -= ___VisualCursorElement.transform.forward * 0.003f;
        }

        [HarmonyPatch(typeof(Tooltip), "LateUpdate")]
        [HarmonyPostfix]
        public static void FixTooltipClipping(Unity_Overlay ___TooltipOverlay)
        {
            if (!IsEnable() || !IsOverlayClipping) return;

            string targetName = EventBridge.GetCurrentHoveringOverlay()?.overlayName;
            if (targetName == "window.settings" || targetName == "window.toolbar")
                ___TooltipOverlay.transform.position -= ___TooltipOverlay.transform.forward * 0.01f;
            else
                ___TooltipOverlay.transform.position -= ___TooltipOverlay.transform.forward * 0.003f;
        }

        [HarmonyPatch(typeof(OpenVR), nameof(OpenVR.Init))]
        [HarmonyPostfix]
        public static void SteamVRConnected(CVRSystem __result, ref EVRInitError peError)
        {
            if (peError == EVRInitError.None && __result != null)
                if (Version.TryParse(__result.GetRuntimeVersion(), out Version currentVersion))
                    IsOverlayClipping = currentVersion > SteamVR_TargetVersion;
        }

        private static bool IsEnable()
        {
            return true;
        }
    }
}
