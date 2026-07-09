using HarmonyLib;
using System.Collections;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class OverlayCurveAutoRefresh
    {
        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPostfix]
        public static void SetSetting(string name, string value, string value1, bool sendAnalytics = true)
        {
            if (!IsEnable()) return;

            switch (name)
            {
                case "CurvedOverlays":
                    Plugin.Instance.StartCoroutine(RefreshAll());
                    break;
                case "OverlayCurveBias":
                    Plugin.Instance.StartCoroutine(RefreshAll());
                    break;
            }
        }

        [HarmonyPatch(typeof(WindowComponentManager), "RecalcWindowScaling")]
        [HarmonyPostfix]
        public static void OverlayScaleChangedAfterChangingWindowProcess(ref Unity_Overlay ___ThisOverlay)
        {
            if (!IsEnable()) return;

            WindowMovementManager.DetermineIfOverlayShouldBeCurved("none", ___ThisOverlay, true);
        }

        [HarmonyPatch(typeof(Raycaster), "Scale")]
        [HarmonyPostfix]
        public static void ListenOverlayScaling(Raycaster __instance)
        {
            if (!IsEnable()) return;

            if (__instance?.HeldOverlay?.IsWindowInteractionLocked == false && __instance?.HeldOverlay?.IsBeingScaled == true)
                WindowMovementManager.DetermineIfOverlayShouldBeCurved("none", __instance.HeldOverlay, true);
        }

        private static IEnumerator RefreshAll()
        {
            yield return null;

            foreach (Unity_Overlay allOverlay in Overlay_Manager.Instance.AllSceneOverlays)
                WindowMovementManager.DetermineIfOverlayShouldBeCurved("none", allOverlay, true);
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayCurveAutoRefresh.Value;
        }
    }
}
