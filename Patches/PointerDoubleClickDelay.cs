using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class PointerDoubleClickDelay
    {
        // Data container unique to each Raycaster CurrentRaycaster
        public class RaycasterState
        {
            public readonly WaitForTime ClickedTimer = new(XSettingsManager.Instance.Settings.DoubleClickDelay);
            public Vector3 SavedDirection;
            public Vector3 SavedPosition;
        }

        // The table that links Raycasters to their specific state
        public static readonly ConditionalWeakTable<Raycaster, RaycasterState> InstanceState = new();

        // Fast field access for private Raycaster variables
        private static readonly AccessTools.FieldRef<Raycaster, Vector3> DirRef = AccessTools.FieldRefAccess<Raycaster, Vector3>("CurrentRayDirection");

        [HarmonyPatch("HandleClicksForDesktopWindows"), HarmonyPatch("HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static void StartBlock(Raycaster __instance)
        {
            if (!IsEnable()) return;

            // Only lock if this CurrentRaycaster is the one currently providing input
            if (DesktopCursorManager.Instance.GetCurrentInputDevice() == __instance)
            {
                RaycasterState state = InstanceState.GetOrCreateValue(__instance);

                // Store current ray state into the CurrentRaycaster-specific state object
                state.SavedDirection = DirRef(__instance);
                state.SavedPosition = __instance.transform.position;
                state.ClickedTimer.WaitTime = XSettingsManager.Instance.Settings.DoubleClickDelay;
                state.ClickedTimer.Reset();
            }
        }

        [HarmonyPatch("SetVisualCursorTransform"), HarmonyPatch("PointerHoverAndStateManagement")]
        [HarmonyPrefix]
        public static bool BlockPointerAndCursor(Raycaster __instance)
        {
            if (!IsEnable()) return true;

            // If we have a state for this hand, check if the timer is still running
            if (InstanceState.TryGetValue(__instance, out RaycasterState state))
            {
                // If timer is NOT ready, return false to skip (block) the original method
                return state.ClickedTimer.IsReady;
            }

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.PointerDoubleClickDelay.Value;
        }
    }
}