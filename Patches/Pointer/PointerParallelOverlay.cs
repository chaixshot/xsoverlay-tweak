using HarmonyLib;
using UnityEngine;
using Valve.VR;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;
using xsoverlay_tweak.Patches.Overlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class PointerParallelOverlay
    {
        [HarmonyPatch("SearchForOverlays")]
        [HarmonyPostfix]
        public static void CursorParallelToCurveoverlay(
            Raycaster __instance,
            VROverlayIntersectionResults_t ovrIntersectionResults,
            MouseInputDevice ___InputDevice,
            ref GameObject ___VisualCursorElement,
            ref GameObject ___VisualCursorElementClickAnimation,
            ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            Unity_Overlay targetOverlay = __instance.HoveringOverlay;
            if (targetOverlay != null)
                if (IsEnable() || WindowsCursorPointer.CursorDictionary.TryGetValue(__instance, out WindowsCursorPointer.CursorData Data) && Data.IsCursor)
                {
                    PullTriggerPointerLock.InstanceState.TryGetValue(__instance, out PullTriggerPointerLock.RaycasterState ClickState);

                    if (!___InputDevice.ClickFreezeActive && (ClickState == null || !ClickState.IsBlock))
                    {
                        Transform transform = targetOverlay.transform;
                        Quaternion rotation = targetOverlay.transform.rotation;

                        if (targetOverlay?.WorldSpaceSceneImpostor != null) // Overlay attached to device
                        {
                            transform = targetOverlay.WorldSpaceSceneImpostor.transform;
                            rotation = targetOverlay.WorldSpaceSceneImpostor.transform.rotation;

                            if (OverlayAttachSmooth.OverlayStatus.TryGetValue(targetOverlay, out var SmoothData)) // Attached device rolling lock
                                if (SmoothData.LockRoll)
                                    rotation = SmoothData.Rotation;
                        }

                        if (targetOverlay.overlayCurveRadius.Equals(0)) // Overlay not curve
                        {
                            ___VisualCursorElement.transform.rotation = rotation;
                        }
                        else // Cursor faces up to the targetOverlay curved surface
                        {
                            Vector3 localNormal = new(ovrIntersectionResults.vNormal.v0, ovrIntersectionResults.vNormal.v1, ovrIntersectionResults.vNormal.v2);
                            Vector3 worldNormal = transform.TransformDirection(localNormal);

                            worldNormal.x = -worldNormal.x; // Mirror X in world space to align with Unity's coordinate system for the cursor plate.

                            // Calculate the tilt required to stay parallel to the curved surface at this specific point.
                            Quaternion surfaceTilt = Quaternion.FromToRotation(Vector3.forward, worldNormal);

                            // Apply the surface tilt to the targetOverlay's base world rotation.
                            ___VisualCursorElement.transform.rotation = rotation * surfaceTilt;
                        }

                    }

                    if (___VisualCursorElementClickAnimationOverlay.gameObject.activeSelf)
                    {
                        ___VisualCursorElementClickAnimation.transform.position = ___VisualCursorElement.transform.position;
                        ___VisualCursorElementClickAnimation.transform.rotation = ___VisualCursorElement.transform.rotation;
                    }
                }
        }

        private static bool IsEnable()
        {
            return true;
        }
    }
}
