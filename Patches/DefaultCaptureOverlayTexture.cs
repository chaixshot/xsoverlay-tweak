using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace xsoverlay_tweak.Patches
{
    internal class DefaultCaptureOverlayTexture
    {
        private class OverlayData
        {
            public Texture2D Texture;
        }

        private static readonly ConditionalWeakTable<Unity_Overlay, OverlayData> OverlayDictionary = new();

        [HarmonyPatch(typeof(WindowComponentManager), "SetupWindow")]
        [HarmonyPostfix]
        public static void CreateOverlayTexture(ref Unity_Overlay ___ThisOverlay)
        {
            if (!IsEnable()) return;

            Texture2D Texture = new(___ThisOverlay.renderTexWidthOverride, ___ThisOverlay.renderTexHeightOverride, TextureFormat.RGB24, false);

            ___ThisOverlay.overlayTexture = Texture;
            ___ThisOverlay.overlay.overlayTexture = Texture;

            OverlayDictionary.Add(___ThisOverlay, new OverlayData { Texture = Texture });
        }

        [HarmonyPatch(typeof(WindowComponentManager), "SetNormalizedScale")]
        [HarmonyPostfix]
        public static void FirstNormalizeCleanip(ref Unity_Overlay ___ThisOverlay)
        {
            if (OverlayDictionary.TryGetValue(___ThisOverlay, out OverlayData Data))
            {
                GameObject.Destroy(Data.Texture);
                OverlayDictionary.Remove(___ThisOverlay);
            }
        }

        private static bool IsEnable()
        {
            return XConfig.DefaultCaptureOverlayTexture.Value;
        }
    }
}
