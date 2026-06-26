using HarmonyLib;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.CommunityRequest
{
    internal class WindowToolbarKeyboard
    {
        private static bool WasEnable = false;

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
        public static void WindowToolbarLoaded(OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.WindowToolbar)
            {
                ChangeWidth(wv);

                wv._webView.WebView.LoadProgressChanged += (sender, args) =>
                {
                    if (args.Type == ProgressChangeType.Finished)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);

                            ChangeUI(wv);
                        });
                    }
                };
            }
        }

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.EnableKeyboard))]
        [HarmonyPostfix]
        public static void SpawnKeyboardPostionFix(Overlay_Manager __instance, KeyboardGlobalManager ___keyboardManager)
        {
            if (!IsEnable()) return;

            if (EventBridge.CurrentHoveringOverlay?.overlayName == "window.toolbar" && ___keyboardManager?.HasKeyboardBeenOpened == true)
            {
                if (!Overlay_Manager.Instance.WindowSettingsMenuParentOverlay.IsAttachedToDevice || !(Overlay_Manager.Instance.WindowSettingsMenuParentOverlay.WorldSpaceSceneImpostor == null))
                {
                    Unity_Overlay windowToolbarOverlay = Overlay_Manager.Instance.WindowToolbar.GetComponentInChildren<Unity_Overlay>(true);
                    WindowMovementManager.MoveToEdgeOfWindowAndInheritRotation(__instance.Keyboard_Overlay, windowToolbarOverlay, 0.1f, 0.1f, 0);
                }
                else
                {
                    __instance.Keyboard_Overlay.transform.position = __instance.head.transform.position + __instance.head.transform.forward * 0.5f;
                    __instance.Keyboard_Overlay.transform.rotation = __instance.head.transform.rotation;
                }
            }
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
                jsCode = @"(function() {
                    if (window.windowToolbarLookup && !window.windowToolbarLookup.Keyboard) {
                        window.windowToolbarLookup = { ""Keyboard"": ""keyboard-fill"", ...window.windowToolbarLookup };
                        var container = document.getElementById('ToolbarButtons') || document.querySelector('.toolbar');
                        if (container && typeof window.InitializeUI === 'function') {
                            container.innerHTML = '';
                            window.InitializeUI();
                        }
                    }
                })();";
            else
                jsCode = @"(function() {
                    if (window.windowToolbarLookup && window.windowToolbarLookup.Keyboard) {
                        delete window.windowToolbarLookup.Keyboard;
                        var container = document.getElementById('ToolbarButtons') || document.querySelector('.toolbar');
                        if (container && typeof window.InitializeUI === 'function') {
                            container.innerHTML = '';
                            window.InitializeUI();
                        }
                    }
                })();";

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
