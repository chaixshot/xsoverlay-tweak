using HarmonyLib;
using System.Threading.Tasks;
using XSOverlay;
using XSOverlay.WebApp;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Wrist
{
    internal class WristStateRestore
    {

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForChanging()
        {
            XSOEventSystem.OnStartStopPerformanceMonitor += (enable) =>
            {
                if (!IsEnable()) return;

                CustomSettings.Settings.IsPerformanceMonitorOpened = enable;
                CustomSettings.SaveSettings();
            };

            CustomAPI.OnToggleMediaPlayer += (enable, byClick) =>
            {
                if (!IsEnable() || !byClick) return;

                CustomSettings.Settings.IsMediaPlayerOpened = enable;
                CustomSettings.SaveSettings();
            };
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void RestoreWristState(OverlayWebView wv)
        {
            if (!IsEnable()) return;

            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Wrist)
            {
                wv.WebViewReady += (IWebView) =>
                {
                    string jsCode = string.Format(
                        @"(function restoreWristToolbarState() {{
                            if (window.toolbarContext) {{
                                const ctx = window.toolbarContext;

                                // Restore Performance Stats state
                                if ({0} && !ctx.state?.ShowPerformanceMonitor) {{
                                    if (ctx.state?.MiniToolbar?.PerformanceStats) {{
                                        ctx.state.MiniToolbar.PerformanceStats.click();
                                    }} else if (ctx.api?.Send) {{
                                        ctx.api.Send('TogglePerformanceStats', null, null);
                                    }}
                                }}

                                // Restore Media Player state
                                if ({1} && !ctx.state?.ShowMediaPlayer) {{
                                    if (typeof ctx.onToggleMediaPlayer === 'function') {{
                                        ctx.onToggleMediaPlayer();
                                    }} else if (ctx.state?.MiniToolbar?.MediaPlayer) {{
                                        ctx.state.MiniToolbar.MediaPlayer.click();
                                    }}
                                }}
                            }} else {{
                                setTimeout(restoreWristToolbarState, 50);
                            }}
                        }})();",
                        CustomSettings.Settings.IsPerformanceMonitorOpened.ToString().ToLower(),
                        CustomSettings.Settings.IsMediaPlayerOpened.ToString().ToLower()
                    );

                    Task.Run(async () =>
                    {
                        await Task.Delay(1500); // Wait for CustomAPI execute listener first

                        wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                        {
                            //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                        });
                    });
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.WristStateRestore.Value;
        }
    }
}
