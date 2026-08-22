using HarmonyLib;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;

namespace xsoverlay_tweak.Patches.Keyboard
{
    internal class KeyboardHoldingIndicator
    {
        private const string styleId = "KeyboardHoldingIndicator";
        private static IWebView _keyboardWebView;

        private const string cssJS = @"
            (function() {
                if (!document.head) return;
                const id = '" + styleId + @"';
                let style = document.getElementById(id);
                if (!style) {
                    style = document.createElement('style');
                    style.id = id;
                    document.head.appendChild(style);
                }
                style.textContent = `
                    /* Key Active (Separated) */
                    #keyboard-container .key.active {
                        box-shadow: 0 0 0 2px var(--theme-accent) !important;
                        scale: 0.9 !important;
                        z-index: 105 !important;
                    }

                    /* Faster press animation */
                    #keyboard-container .keyboard-control {
                        transition: all 0.025s ease-out !important;
                    }

                    /* Override framework high specificity on button click/press */
                    #keyboard-container .keyboard-control:active,
                    #keyboard-container .keyboard-control.active,
                    #keyboard-container .keyboard-control.pressed {
                        transform: scale(0.9) !important;
                        scale: 0.9 !important;
                        background-color: color-mix(in srgb, var(--theme-hi) 60%, var(--theme-dark)) !important;
                    }
                `;
            })();";

        private const string undoJS = @"
            (function() {
                const style = document.getElementById('" + styleId + @"');
                if (style) {
                    style.remove();
                }
            })();";

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenConfigChange()
        {
            XConfig.KeyboardHoldingIndicator.SettingChanged += (sender, args) =>
            {
                UpdateStyleState();
            };
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewOverlay(OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Keyboard)
            {
                IWebView webView = wv._webView.WebView;

                webView.LoadProgressChanged += (s, e) =>
                {
                    if (e.Type == ProgressChangeType.Finished)
                    {
                        _keyboardWebView = webView;
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            UpdateStyleState();
                        });
                    }
                };
            }
        }

        public static void UpdateStyleState()
        {
            if (_keyboardWebView == null) return;

            if (IsEnable())
                _keyboardWebView.ExecuteJavaScript(cssJS, null);
            else
                _keyboardWebView.ExecuteJavaScript(undoJS, null);
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardHoldingIndicator.Value;
        }
    }
}