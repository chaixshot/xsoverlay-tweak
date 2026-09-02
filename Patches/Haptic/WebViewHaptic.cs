using HarmonyLib;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class WebViewHaptic
    {
        private const string HapticJS = @"
            (function() {
                if (window.XSOverlayTweak_Haptic) return;
                window.XSOverlayTweak_Haptic = true;

                const selector = '.side-bar-button, .button, .settings-button, .settings-button-basic, .switch, .slider-container, .select, .selectopt, .dropdown-item, .dropdown-button, .item-list-opt-list';
                let wasOverScrollbar = false;
                let lastMove = 0;

                document.addEventListener('mouseover', (e) => {
                    const target = e.target.closest(selector);
                    if (target && !target.contains(e.relatedTarget)) {
                        window.vuplex.postMessage('XSOverlayTweak-Haptic-Hover');
                    }
                }, true);

                document.addEventListener('mousemove', (e) => {
                    const now = Date.now();
                    if (now - lastMove < 50) return; 
                    lastMove = now;

                    const t = e.target;
                    if (!t || !t.getBoundingClientRect) return;

                    const hasScrollX = t.offsetWidth > t.clientWidth;
                    const hasScrollY = t.offsetHeight > t.clientHeight;
                    if (!hasScrollX && !hasScrollY) { wasOverScrollbar = false; return; }

                    const rect = t.getBoundingClientRect();
                    const isOver = (hasScrollY && e.clientX >= rect.left + t.clientWidth) || 
                                   (hasScrollX && e.clientY >= rect.top + t.clientHeight);

                    if (isOver && !wasOverScrollbar)
                        window.vuplex.postMessage('XSOverlayTweak-Haptic-Hover');
                        
                    wasOverScrollbar = isOver;
                }, true);
            })();";

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewLoaded(OverlayWebView wv)
        {
            wv.WebViewReady += (IWebView) =>
            {
                IWebView webView = wv._webView.WebView;
                string overlayName = wv._overlay.overlayName;

                // Listen for messages from the injected JavaScript
                webView.MessageEmitted += (sender, args) =>
                {
                    if (!IsEnable() || args.Value != "XSOverlayTweak-Haptic-Hover") return;

                    Raycaster targetRaycaster = overlayName == "keyboard" ? EventBridge.GetActiveKeyboardRaycaster() : EventBridge.GetActiveWebViewRaycaster();
                    AdvancedHaptics.Rumble(targetRaycaster?.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.WebViewHaptic.Value / 100f);
                };

                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    webView.ExecuteJavaScript(HapticJS, (result) =>
                    {
                        //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                    });
                });
            };
        }

        private static bool IsEnable()
        {
            return XConfig.WebViewHaptic.Value != 0;
        }
    }
}
