using HarmonyLib;
using System.Threading.Tasks;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class WindowToolbarKeyboard
    {
        private static bool WasEnable = false;
        private static bool toolbarKeyboardClicked = false;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void AddWindowToolbarKeybordButton(ApiHandler __instance)
        {
            XConfig.WindowToolbarKeyboard.SettingChanged += (sender, args) =>
            {
                OverlayWebView webView = Overlay_Manager.Instance.WindowToolbar.GetComponentInChildren<Unity_Overlay>(true)?.OverlayWebView;

                if (webView != null)
                {
                    ChangeUI(webView);
                    ChangeWidth(webView);

                    webView._webView.WebView.SetRenderingEnabled(true);
                }
            };
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewWindowToolbarLoaded(OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.WindowToolbar)
            {
                ChangeWidth(wv);

                wv.WebViewReady += (IWebView) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);

                        ChangeUI(wv);
                    });
                };
            }
        }

        [HarmonyPatch(typeof(Raycaster), "CompleteCapturedWebViewPress")]
        [HarmonyPostfix]
        public static void ClickToolbarKeyboard(Raycaster __instance, bool __result)
        {
            if (!IsEnable()) return;

            // Check if the click/press actually succeeded and targeted the toolbar
            if (__result && __instance.HoveringOverlay?.overlayName == "window.toolbar")
            {
                toolbarKeyboardClicked = true;
            }
        }

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.EnableKeyboard))]
        [HarmonyPostfix]
        public static void SpawnKeyboardPostionFix(Overlay_Manager __instance, bool ___KeyboardShouldBeVisible)
        {
            if (!IsEnable()) return;

            if (toolbarKeyboardClicked && ___KeyboardShouldBeVisible)
            {
                if (!Overlay_Manager.Instance.WindowSettingsMenuParentOverlay.IsAttachedToDevice || !(Overlay_Manager.Instance.WindowSettingsMenuParentOverlay.WorldSpaceSceneImpostor == null))
                {
                    Unity_Overlay windowToolbarOverlay = Overlay_Manager.Instance.WindowToolbar.GetComponentInChildren<Unity_Overlay>(true);
                    WindowMovementManager.MoveToEdgeOfWindowAndInheritRotation(__instance.Keyboard_Overlay, windowToolbarOverlay, 0.6f, 0f, 0);
                }
                else
                {
                    __instance.Keyboard_Overlay.transform.position = __instance.head.transform.position + __instance.head.transform.forward * 0.5f;
                    __instance.Keyboard_Overlay.transform.rotation = __instance.head.transform.rotation;
                }
            }

            toolbarKeyboardClicked = false;
        }

        private static void ChangeWidth(OverlayWebView toolbarWebView)
        {
            float targetWidth = toolbarWebView.Width;
            bool isChanged = false;

            if (IsEnable())
            {
                isChanged = true;
                WasEnable = true;
                targetWidth += 110;
            }
            else if (WasEnable)
            {
                isChanged = true;
                WasEnable = false;
                targetWidth -= 110;
            }

            if (isChanged)
            {
                toolbarWebView.Width = targetWidth;

                toolbarWebView.UpdateResolution(new UnityEngine.Resolution { width = (int)toolbarWebView.Width, height = (int)toolbarWebView.Height });
            }
        }

        private static void ChangeUI(OverlayWebView toolbarWebView)
        {
            var webView = toolbarWebView._webView.WebView;
            if (webView == null) return;

            string jsCode;

            if (IsEnable())
            {
                jsCode = @"(function() {
                    // Locate the main toolbar container
                    var toolbar = document.querySelector('.toolbar');
                    if (!toolbar || document.getElementById('Keyboard')) return;

                    // Store reference to the current leftmost button to update its styling
                    var firstButton = toolbar.querySelector('.button');

                    // Create the Keyboard button element and apply left-side rounded corners
                    var button = document.createElement('button');
                    button.id = 'Keyboard';
                    button.className = 'button buttonL';

                    // Create the wrapper container for the button icon
                    var imgContainer = document.createElement('div');
                    imgContainer.className = 'button-image-container';

                    // Create the Bootstrap Icon element using a div
                    var icon = document.createElement('div');
                    icon.className = 'bi-keyboard-fill';

                    imgContainer.appendChild(icon);
                    button.appendChild(imgContainer);

                    // Create a vertical divider to separate the Keyboard button from adjacent items
                    var divider = document.createElement('div');
                    divider.className = 'toolbar-divider';
                    divider.id = 'Keyboard-divider';

                    // Prepend the button and divider to make them the first item on the left
                    toolbar.prepend(divider);
                    toolbar.prepend(button);

                    // Strip the left rounded corner class from the previous first button
                    if (firstButton) {
                        firstButton.classList.remove('buttonL');
                    }

                    // Bind click listener to dispatch the 'Keyboard' command to the XSOverlay API
                    button.addEventListener('click', function (e) {
                        setTimeout(function () { button.blur(); }, 150);
                        if (window.toolbarContext?.api) {
                            window.toolbarContext.api.Send('Keyboard', null, null);
                        }
                        e.preventDefault();
                    });

                    // Show tooltip on hover
                    button.addEventListener('mouseenter', function () {
                        if (window.toolbarContext?.api) {
                            window.toolbarContext.api.Send(window.toolbarContext.api.Commands.ShowTooltip, 'Keyboard', true);
                        }
                    });

                    // Hide tooltip on mouse leave
                    button.addEventListener('mouseleave', function () {
                        if (window.toolbarContext?.api) {
                            window.toolbarContext.api.Send(window.toolbarContext.api.Commands.ShowTooltip, null, false);
                        }
                    });
                })();";
            }
            else
            {
                jsCode = @"(function() {
                    // Remove the injected Keyboard button and divider elements
                    var btn = document.getElementById('Keyboard');
                    var div = document.getElementById('Keyboard-divider');
                    if (btn) btn.remove();
                    if (div) div.remove();

                    // Re-apply left-side rounded corners to the new first button in the toolbar
                    var toolbar = document.querySelector('.toolbar');
                    if (toolbar) {
                        var firstButton = toolbar.querySelector('.button');
                        if (firstButton) {
                            firstButton.classList.add('buttonL');
                        }
                    }
                })();";
            }

            toolbarWebView._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
            {
                //Plugin.Logger.LogError($"[{toolbarWebView.UserInterfaceSelection}] {result}");
            });
        }

        private static bool IsEnable()
        {
            return XConfig.WindowToolbarKeyboard.Value;
        }
    }
}
