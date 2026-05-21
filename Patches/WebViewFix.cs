using HarmonyLib;
using System.Collections.Generic;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    internal class WebViewFix
    {
        private static readonly List<Unity_Overlay> DisabledOverlays = [];

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void OnSwitchHoveringOverlay(DeviceManager __instance)
        {
            XSOEventSystem.OnSwitchHoveringOverlay += (hover, overlay) =>
            {
                if (!IsEnable()) return;

                if (overlay?.WebViewHandler && overlay.IsPluginApplication && !overlay.IsDesktopOrWindowCapture)
                    overlay.OverlayWebView._webView.WebView.SetRenderingEnabled(true);
            };
        }

        [HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool Pre(Raycaster __instance)
        {
            if (!IsEnable()) return true;

            foreach (Unity_Overlay allSceneOverlay in Overlay_Manager.Instance.AllSceneOverlays)
            {
                if (allSceneOverlay?.WebViewHandler && allSceneOverlay.IsPluginApplication && !allSceneOverlay.IsDesktopOrWindowCapture)
                    if (allSceneOverlay != __instance.HoveringOverlay)
                        if (!allSceneOverlay.overlayName.Equals("wrist") && !allSceneOverlay.overlayName.Equals("notification"))
                        {
                            allSceneOverlay.OverlayWebView._webView.WebView.SetRenderingEnabled(false);

                            DisabledOverlays.Add(allSceneOverlay);
                        }
            }

            foreach (Unity_Overlay allSceneOverlay in Overlay_Manager.Instance.AllSceneOverlays)
                if (allSceneOverlay == __instance.HoveringOverlay)
                    if (allSceneOverlay?.WebViewHandler && allSceneOverlay.IsPluginApplication && !allSceneOverlay.IsDesktopOrWindowCapture)
                        allSceneOverlay.OverlayWebView._webView.WebView.SetRenderingEnabled(true);

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPostfix]
        public static void Post()
        {
            if (!IsEnable()) return;

            foreach (Unity_Overlay item in DisabledOverlays)
            {
                if (item.overlayName.Equals("window.settings"))
                    item.OverlayWebView._webView.WebView.SetRenderingEnabled(true);
            }

            DisabledOverlays.Clear();
        }

        private static bool IsEnable()
        {
            return XConfig.WebViewFix.Value;
        }
    }
}
