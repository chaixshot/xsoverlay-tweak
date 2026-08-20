using HarmonyLib;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class KeyboardKeyHaptic
    {
        private const string HapticJS = @"
            (function hook() {
                if (!window.SetPointerHover) return setTimeout(hook, 100);
                if (window.XSOverlayTweak_KeyboardKeyHaptic) return;
                window.XSOverlayTweak_KeyboardKeyHaptic = true;

                window._origHover = window.SetPointerHover;
                window.SetPointerHover = (pointerId, nextElement) => {
                    const isNew = nextElement && window.GetKeyboardPointer?.(pointerId)?.hoverElement !== nextElement;
                    window._origHover(pointerId, nextElement);
                    if (isNew) window.vuplex.postMessage('XSOverlayTweak-KeyboardHaptic-Hover:' + pointerId);
                };
            })();";

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewOverlay(OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Keyboard)
            {
                IWebView webView = wv._webView.WebView;

                // Listen for messages from the injected JavaScript
                webView.MessageEmitted += (sender, args) =>
                {
                    if (!IsEnable() || !args.Value.StartsWith("XSOverlayTweak-KeyboardHaptic-Hover:")) return;

                    string pointerId = args.Value.Substring("XSOverlayTweak-KeyboardHaptic-Hover:".Length);
                    AdvancedHaptics.Rumble(pointerId == "1", 0.001f, 320f, XConfig.KeyboardKeyHaptic.Value / 100f);
                };

                // Inject the script when loading completes
                webView.LoadProgressChanged += (s, e) =>
                {
                    if (e.Type == ProgressChangeType.Finished)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            webView.ExecuteJavaScript(HapticJS, null);
                        });
                    }
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardKeyHaptic.Value != 0;
        }
    }
}
