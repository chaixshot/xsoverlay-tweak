using HarmonyLib;
using System.Collections;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class WebViewFix
    {
        private static Coroutine StoppingCoroutine;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void OnSwitchHoveringOverlay()
        {
            XSOEventSystem.OnSwitchHoveringOverlay += (raycaster, overlay) =>
            {
                if (!IsEnable()) return;

                if (EventBridge.IsOverlayWebView(overlay))
                {
                    if (StoppingCoroutine != null)
                        Plugin.Instance.StopCoroutine(StoppingCoroutine);
                    StoppingCoroutine = Plugin.Instance.StartCoroutine(StoppingDelay(overlay));

                    overlay.OverlayWebView._webView.WebView.SetRenderingEnabled(true);
                }
            };
        }

        //!! Not work by clicking
        /*[HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool ClickWebView(Raycaster __instance)
        {
            if (!IsEnable()) return true;

            Plugin.Logger.LogError(__instance.HoveringOverlay.overlayName);
            if (IsWebView(__instance.HoveringOverlay))
            {
                if (StopingCoroutine != null)
                    Plugin.Instance.StopCoroutine(StopingCoroutine);
                StopingCoroutine = Plugin.Instance.StartCoroutine(StopingDelay(__instance.HoveringOverlay));

                __instance.HoveringOverlay.OverlayWebView._webView.WebView.SetRenderingEnabled(true);
            }

            return true;
        }*/

        [HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPostfix]
        public static void WindowSettingsSwitch(Raycaster __instance)
        {
            if (!IsEnable()) return;

            if (__instance.HoveringOverlay.overlayName.Equals("window.toolbar"))
                foreach (Unity_Overlay allOverlay in Overlay_Manager.Instance.AllSceneOverlays)
                    if (allOverlay.overlayName.Equals("window.settings"))
                    {
                        allOverlay.OverlayWebView._webView.WebView.SetRenderingEnabled(true);
                        break;
                    }
        }

        private static IEnumerator StoppingDelay(Unity_Overlay overlay)
        {
            yield return new WaitForSecondsRealtime(0.22f);
            foreach (Unity_Overlay allOverlay in Overlay_Manager.Instance.AllSceneOverlays)
                if (EventBridge.IsOverlayWebView(allOverlay))
                    if (allOverlay != overlay)
                        allOverlay.OverlayWebView._webView.WebView.SetRenderingEnabled(false);
        }

        private static bool IsEnable()
        {
            return XConfig.WebViewFix.Value;
        }
    }
}
