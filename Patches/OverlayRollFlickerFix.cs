using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    internal class OverlayRollFlickerFix
    {
        private class RollState { public float LastRotation; }
        private static readonly ConditionalWeakTable<Unity_Overlay, RollState> LastX = new();

        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.HandleWindowRollAndRotation))]
        [HarmonyPostfix]
        public static void MoveDownFromAboveFlicker(ref Transform overlayTransform, ref Unity_Overlay overlayToPoint)
        {
            if (!IsEnable()) return;

            RollState state = LastX.GetOrCreateValue(overlayToPoint);
            Quaternion rotation = overlayTransform.rotation;

            if (rotation.eulerAngles.x > 335f)
                if (rotation.eulerAngles.x > state.LastRotation)
                    overlayTransform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z);

            state.LastRotation = rotation.eulerAngles.x;
        }

        [HarmonyPatch(typeof(WindowMovementManager), nameof(WindowMovementManager.DetermineIfOverlayShouldBeCurved))]
        [HarmonyPostfix]
        public static void CurveApplyFlicker(ref Unity_Overlay overlay)
        {
            if (!IsEnable()) return;
            if (overlay.IsHeld) return;

            Quaternion rotation = overlay.transform.rotation;

            if (rotation.eulerAngles.x > 335f)
                overlay.transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z);
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayRollFlickerFix.Value && XSettingsManager.Instance.Settings.CurvedOverlays;
        }
    }
}
