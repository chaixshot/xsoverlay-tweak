using HarmonyLib;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Wrist
{
    [HarmonyPatch(typeof(Unity_Overlay))]
    internal class WristClipDistance
    {
        [HarmonyPatch("AutoHideOverlayBasedOnHeadAngle")]
        [HarmonyPostfix]
        public static void AutoHideOverlayBasedOnHeadDistance(Unity_Overlay __instance)
        {
            if (!__instance.IsWristOverlay) return;

            Transform transform = __instance.transform;

            if (__instance.WorldSpaceSceneImpostor != null)
                transform = __instance.WorldSpaceSceneImpostor.transform;

            float dist = Vector3.Distance(Overlay_Manager.Instance.head.transform.position, transform.position);
            if (dist >= XConfig.WristClipDistance.Value * EventBridge.OneCentimetre)
            {
                if (!__instance.IsHidden)
                    AccessTools.Method(typeof(Unity_Overlay), "FadeOverlayOut").Invoke(__instance, null);
            }
        }
    }
}
