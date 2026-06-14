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

        [HarmonyPatch(typeof(Raycaster), "SetVisualCursorTransform")]
        [HarmonyPostfix]
        public static void FixPointerClipping(Raycaster __instance, ref GameObject ___VisualCursorElement)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            if (IsOverlayClipping)
            {
                Unity_Overlay overlay = __instance.HoveringOverlay;

                if (overlay != null && ___VisualCursorElement != null)
                {
                    // Push Pointer slightly closer to the player's face than Hover Overlay
                    float zBias = 0.001f;
                    ___VisualCursorElement.transform.position += overlay.transform.forward * -zBias; // -Z is forward toward the headset in OpenVR space}
                }
            }
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
