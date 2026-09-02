using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using XSOverlay;
using XSOverlay.WebApp;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    internal class WebViewWiderScroll
    {
        private static readonly List<OverlayWebView> WebViews = [];

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            // Listen to edit mode change
            XConfig.WebViewWiderScroll.SettingChanged += (sender, args) =>
            {
                foreach (OverlayWebView item in WebViews)
                {
                    if (IsEnable())
                        AddCSS(item);
                    else
                        RemoveCSS(item);
                }
            };
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewWindowSettingsLoaded(OverlayWebView wv)
        {
            if (!IsEnable()) return;

            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Settings || wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.WindowSettings)
            {
                wv.WebViewReady += (IWebView) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);

                        if (!WebViews.Contains(wv))
                            WebViews.Add(wv);

                        AddCSS(wv);
                    });
                };
            }
        }

        public static void AddCSS(OverlayWebView wv)
        {
            string styleId = GetStyleId();
            string jsCode = string.Format(@"
            (function() {{
                if (!document.head) return 'ERROR: No Head';
                const id = '{0}';
                let style = document.getElementById(id);
                if (!style) {{
                    style = document.createElement('style');
                    style.id = id;
                    document.head.appendChild(style);
                }}
                style.innerHTML = `
                    ::-webkit-scrollbar {{
				        width: 17px;
			        }}
                `;
                return 'SUCCESS: Applied ' + id;
            }})();", styleId);

            wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
            {
                //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
            });
        }

        public static void RemoveCSS(OverlayWebView wv)
        {
            string styleId = GetStyleId();
            string jsCode = $@"
            (function() {{
                const style = document.getElementById('{styleId}');
                if (style) {{
                    style.remove();
                    return 'SUCCESS: Removed ' + '{styleId}';
                }}
                return 'SUCCESS: Not found';
            }})();";

            wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
            {
                //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
            });
        }

        private static string GetStyleId()
        {
            return "xso-tweak-scrollbar";
        }

        private static bool IsEnable()
        {
            return XConfig.WebViewWiderScroll.Value;
        }
    }
}
