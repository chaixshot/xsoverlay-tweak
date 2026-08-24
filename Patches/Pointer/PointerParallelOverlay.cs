using HarmonyLib;
using UnityEngine;
using Valve.VR;
using XSOverlay;
using xsoverlay_tweak.Patches.Cursor;
using xsoverlay_tweak.Patches.Overlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Pointer
{
    internal class PointerParallelOverlay
    {
        [HarmonyPatch(typeof(Raycaster), "SearchForOverlays")]
        [HarmonyPostfix]
        public static void PointerParallelToTargetOverlay(
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
                if (IsEnable() || WindowsCursorPointer.IsCursorMode(__instance))
                {
                    PullTriggerPointerLock.InstanceState.TryGetValue(__instance, out PullTriggerPointerLock.RaycasterState ClickState);

                    if (!___InputDevice.ClickFreezeActive && (ClickState == null || !ClickState.IsLocking))
                    {
                        // Toolbar and WindowSettings overlay using parent as target
                        if (targetOverlay.overlayName.Equals("window.settings") || targetOverlay.overlayName.Equals("window.toolbar"))
                            targetOverlay = Overlay_Manager.Instance.WindowToolbarMover.ParentOverlay;

                        Transform transform = targetOverlay.transform;
                        Quaternion rotation = targetOverlay.transform.rotation;
                        bool isAttachedToDevice = targetOverlay?.WorldSpaceSceneImpostor != null || targetOverlay.IsAttachedToDevice;

                        // Overlay attached to device
                        if (isAttachedToDevice)
                        {
                            transform = targetOverlay.WorldSpaceSceneImpostor.transform;
                            rotation = targetOverlay.WorldSpaceSceneImpostor.transform.rotation;

                            OverlayAttachSmooth.IsLockRoll(targetOverlay, ref rotation);
                        }

                        if (isAttachedToDevice) // Overlay attached to device
                            ___VisualCursorElement.transform.rotation = rotation;
                        else if (targetOverlay.overlayCurveRadius.Equals(0)) // Overlay not curve
                            ___VisualCursorElement.transform.rotation = rotation;
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

        [HarmonyPatch(typeof(Tooltip), "LateUpdate")]
        [HarmonyPostfix]
        public static void TooltipParallelToParentOverlay(Unity_Overlay ___TooltipOverlay, Unity_Overlay ___TooltipParent)
        {
            if (___TooltipOverlay != null && ___TooltipParent != null)
                ___TooltipOverlay.transform.rotation = ___TooltipParent.transform.rotation;
        }

        private static bool IsEnable()
        {
            return true;
        }
    }
}
