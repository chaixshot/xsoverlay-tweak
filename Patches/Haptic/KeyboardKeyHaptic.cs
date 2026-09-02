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

                const lastReleaseTime = new Map();

                // Hook ReleaseKeyboardPointer to log when a click release occurs
                if (window.ReleaseKeyboardPointer) {
                    const origRelease = window.ReleaseKeyboardPointer;
                    window.ReleaseKeyboardPointer = (pointerId, removePointer) => {
                        lastReleaseTime.set(pointerId, Date.now());
                        origRelease(pointerId, removePointer);
                    };
                }

                window._origHover = window.SetPointerHover;
                window.SetPointerHover = (pointerId, nextElement) => {
                    const currentHover = window.GetKeyboardPointer?.(pointerId)?.hoverElement;
                    const isNew = nextElement && currentHover !== nextElement;

                    window._origHover(pointerId, nextElement);

                    // Check if native press is currently active (e.g. key is actively down)
                    const touchPressId = 'touch:' + pointerId;
                    const isActivelyPressed = window.Keyboard?.NativePresses?.has(touchPressId);

                    // Cooldown check to ignore the fake 're-hover' right after releasing a click
                    const lastRelease = lastReleaseTime.get(pointerId) || 0;
                    const isJustReleased = (Date.now() - lastRelease) < 60; // 60ms window

                    if (isNew && !isActivelyPressed && !isJustReleased) {
                        window.vuplex?.postMessage('XSOverlayTweak-KeyboardHaptic-Hover:' + pointerId);
                    }
                };
            })();
        ";

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewKeyboardLoaded(OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Keyboard)
            {
                wv.WebViewReady += (IWebView) =>
                {
                    IWebView webView = wv._webView.WebView;

                    // Listen for messages from the injected JavaScript
                    webView.MessageEmitted += (sender, args) =>
                    {
                        if (!IsEnable() || !args.Value.StartsWith("XSOverlayTweak-KeyboardHaptic-Hover:")) return;

                        string pointerId = args.Value.Substring("XSOverlayTweak-KeyboardHaptic-Hover:".Length);
                        AdvancedHaptics.Rumble(pointerId == "1", 0.001f, 320f, XConfig.KeyboardKeyHaptic.Value / 100f);
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
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardKeyHaptic.Value != 0;
        }
    }
}
