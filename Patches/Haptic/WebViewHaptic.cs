using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class WebViewHaptic
    {
        private static readonly ConditionalWeakTable<IWebView, object> InitializedWebViews = new();
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

        [HarmonyPatch("OnCursorPluginApplication")]
        [HarmonyPostfix]
        public static void OnCursorPluginApplication(Raycaster __instance, bool canCursorInteract, MouseInputDevice ___InputDevice)
        {
            if (!IsEnable() || !canCursorInteract || __instance.HoveringOverlay?.WebViewHandler?.WebView == null) return;

            IWebView webView = __instance.HoveringOverlay.WebViewHandler.WebView;

            if (!InitializedWebViews.TryGetValue(webView, out _))
            {
                InitializedWebViews.Add(webView, null);

                // Inject script to detect hovers on specific elements (buttons, sliders, options)
                webView.ExecuteJavaScript(HapticJS);

                // Re-inject on reload
                webView.LoadProgressChanged += (s, e) =>
                {
                    if (e.Type == ProgressChangeType.Finished)
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);

                            webView.ExecuteJavaScript(HapticJS);
                        });
                };

                // Listen for messages from the web side
                webView.MessageEmitted += (sender, args) =>
                {
                    if (IsEnable() && args.Value == "XSOverlayTweak-Haptic-Hover" && __instance.HeldOverlay == null)
                    {
                        if (__instance != null && __instance.HoveringOverlay?.WebViewHandler?.WebView == (IWebView)sender)
                            AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.WebViewHaptic.Value / 100f);
                    }
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.WebViewHaptic.Value != 0;
        }
    }
}
