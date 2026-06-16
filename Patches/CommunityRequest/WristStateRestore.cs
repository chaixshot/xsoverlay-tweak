using HarmonyLib;
using System.Threading.Tasks;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.CommunityRequest
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

            CustomAPI.OnClickToggleMediaPlayer += (enable) =>
            {
                if (!IsEnable()) return;

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
                string jsCode = string.Format(@"(function() {{
                        if ({0}) {{
                            MiniToolbar.PerformanceStats.click();
                        }};

                        if ({1} && !ShowMediaPlayer) {{
                            MiniToolbar.MediaPlayer.click();
                        }};
                    }})()", CustomSettings.Settings.IsPerformanceMonitorOpened.ToString().ToLower(), CustomSettings.Settings.IsMediaPlayerOpened.ToString().ToLower());

                wv._webView.WebView.LoadProgressChanged += (sender, args) =>
                {
                    if (args.Type == ProgressChangeType.Finished)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);

                            wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                            {
                                //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                            });
                        });
                    }
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.WristStateRestore.Value;
        }
    }
}
