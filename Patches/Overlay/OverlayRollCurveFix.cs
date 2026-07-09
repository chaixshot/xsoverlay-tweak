using HarmonyLib;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class OverlayRollCurveFix
    {
        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.DetermineIfOverlayShouldBeCurved))]
        [HarmonyPostfix]
        public static void CurveApplyFlicker(Unity_Overlay overlay)
        {
            // If our mod feature is disabled, stop right here
            if (!IsEnable()) return;

            // Check if the overlay is currently flag-marked as curved by the original method
            if (overlay.OverlayIsWithinCurveRange && (overlay.overlayCurveRadius > 0f || overlay.overlayCachedCurveRadius > 0f || !overlay.IsDoneAdjustingCurve))
            {
                // Get current rotation angles
                Vector3 currentEuler = overlay.transform.rotation.eulerAngles;

                // Normalize the X-axis (Pitch) to standard -180 to 180 degrees
                float pitch = currentEuler.x;
                if (pitch > 180f) pitch -= 360f;

                // Critical zone threshold where SteamVR mesh starts glitching
                float criticalPitchThreshold = -15f;

                // If it is tilted dangerously back OR the user is dragging it around while curved,
                // force the pitch axis perfectly flat (0) while maintaining its Yaw (y) and Roll (z).
                if (pitch < criticalPitchThreshold)
                    overlay.transform.rotation = Quaternion.Euler(0f, currentEuler.y, currentEuler.z);
            }
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayRollCurveFix.Value && XSettingsManager.Instance.Settings.CurvedOverlays;
        }
    }
}
