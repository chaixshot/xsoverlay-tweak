using HarmonyLib;
using System;
using UnityEngine;
using Valve.VR;
using xsoverlay_tweak.Patches.CommunityRequest;
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
        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPostfix]
        public static void FixPointerClipping(
            Raycaster __instance,
            VROverlayIntersectionResults_t ovrIntersectionResults,
            ref GameObject ___VisualCursorElement,
            ref GameObject ___VisualCursorElementClickAnimation,
            ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            if (IsOverlayClipping)
            {
                Unity_Overlay overlay = __instance.HoveringOverlay;

                if (overlay != null && ___VisualCursorElement != null)
                {
                    // Recreate the precise world rotation calculation from your other patch
                    Transform transform = overlay.transform;
                    Quaternion rotation = overlay.transform.rotation;

                    if (overlay?.WorldSpaceSceneImpostor != null) // Handle attached device
                    {
                        transform = overlay.WorldSpaceSceneImpostor.transform;
                        rotation = overlay.WorldSpaceSceneImpostor.transform.rotation;

                        if (OverlayAttachSmooth.OverlayStatus.TryGetValue(overlay, out var SmoothData))
                            if (SmoothData.LockRoll)
                                rotation = SmoothData.Rotation;
                    }

                    Vector3 pushDirection;

                    if (overlay.overlayCurveRadius.Equals(0)) // Flat overlay
                    {
                        // Flat overlays push out along the overlay's true forward vector
                        pushDirection = rotation * Vector3.forward;
                    }
                    else // Curved overlay
                    {
                        Vector3 localNormal = new(ovrIntersectionResults.vNormal.v0, ovrIntersectionResults.vNormal.v1, ovrIntersectionResults.vNormal.v2);
                        Vector3 worldNormal = transform.TransformDirection(localNormal);

                        // Mirror X in world space exactly like your curve patch does
                        worldNormal.x = -worldNormal.x;

                        Quaternion surfaceTilt = Quaternion.FromToRotation(Vector3.forward, worldNormal);

                        // This gives us the exact world direction pointing directly away from the curved face
                        pushDirection = (rotation * surfaceTilt) * Vector3.forward;
                    }

                    // Apply the push away from the screen face (OpenVR Z axis compensation)
                    ___VisualCursorElement.transform.position += pushDirection * -0.0015f;

                    if (___VisualCursorElementClickAnimationOverlay.gameObject.activeSelf)
                        ___VisualCursorElementClickAnimation.transform.position += pushDirection * -0.0001f;
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
