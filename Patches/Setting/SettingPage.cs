using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Patches.Setting
{
    internal class SettingPage
    {
        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void WebviewOverlay(ref OverlayWebView wv)
        {
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Settings)
                InjectSettingsModule(wv);
        }

        [HarmonyPatch(typeof(ApiHandler), "OnRequestCurrentSettings")]
        [HarmonyPostfix]
        public static void OnRequestCurrentSettings(string sender)
        {
            if (!sender.Equals("systemui_settings")) return;

            var settings = new Dictionary<string, object>
            {
                // RefreshRate
                ["XSOverlayTweak.EnableRefreshRate"] = XConfig.EnableRefreshRate.Value,
                ["XSOverlayTweak.RefreshRate"] = XConfig.RefreshRate.Value,

                // Cursor
                ["XSOverlayTweak.AlwayUpdateCursor"] = XConfig.AlwayUpdateCursor.Value,
                ["XSOverlayTweak.AlwaysHideCursor"] = XConfig.AlwaysHideCursor.Value,
                ["XSOverlayTweak.PhysicalMouseDetector"] = XConfig.PhysicalMouseDetector.Value,

                // Pointer
                ["XSOverlayTweak.ActivePointerColor"] = XConfig.ActivePointerColor.Value,
                ["XSOverlayTweak.ActivePointerOpacity"] = XConfig.ActivePointerOpacity.Value,
                ["XSOverlayTweak.PointerScaleMultiply"] = XConfig.PointerScaleMultiply.Value,
                ["XSOverlayTweak.PointerDoubleClickDelay"] = XConfig.PointerDoubleClickDelay.Value,

                // Mouse Navigation
                ["XSOverlayTweak.MouseNavigation"] = XConfig.MouseNavigation.Value,
                ["XSOverlayTweak.MouseNavigationUseModifiedKey"] = XConfig.MouseNavigationUseModifiedKey.Value,

                ["XSOverlayTweak.UpdateNotification"] = XConfig.UpdateNotification.Value,
            };

            var data = JsonConvert.SerializeObject(settings);
            ServerClientBridge.Instance.Api.SendMessage("UpdateSettings", data, null, sender);
        }

        [HarmonyPatch(typeof(XSettingsManager)), HarmonyPatch(nameof(XSettingsManager.SetSetting))]
        [HarmonyPrefix]
        public static bool SetSetting(string name, string value, string value1, bool sendAnalytics = true)
        {
            switch (name)
            {
                // RefreshRate
                case "XSOverlayTweak.EnableRefreshRate":
                    XConfig.EnableRefreshRate.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.RefreshRate":
                    XConfig.RefreshRate.Value = int.Parse(value);
                    break;

                // Cursor
                case "XSOverlayTweak.AlwayUpdateCursor":
                    XConfig.AlwayUpdateCursor.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.AlwaysHideCursor":
                    XConfig.AlwaysHideCursor.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.PhysicalMouseDetector":
                    XConfig.PhysicalMouseDetector.Value = bool.Parse(value);
                    break;

                // Pointer
                case "XSOverlayTweak.ActivePointerColor":
                    XConfig.ActivePointerColor.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.ActivePointerOpacity":
                    XConfig.ActivePointerOpacity.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.PointerScaleMultiply":
                    XConfig.PointerScaleMultiply.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.PointerDoubleClickDelay":
                    XConfig.PointerDoubleClickDelay.Value = bool.Parse(value);
                    break;

                // Mouse Navigation
                case "XSOverlayTweak.MouseNavigation":
                    XConfig.MouseNavigation.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.MouseNavigationUseModifiedKey":
                    XConfig.MouseNavigationUseModifiedKey.Value = bool.Parse(value);
                    break;

                // About
                case "XSOverlayTweak.CheckForUpdate":
                    Task.Run(Utils.Update.CheckForUpdate);
                    break;
                case "XSOverlayTweak.OpenGitHub":
                    Task.Run(Utils.Update.OpenGitHubPage);
                    break;
                case "XSOverlayTweak.UpdateNotification":
                    XConfig.UpdateNotification.Value = bool.Parse(value);
                    break;
            }

            return true;
        }

        public static void InjectSettingsModule(OverlayWebView wv)
        {

            // JS for inserting the actual settings page
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("xsoverlay_tweak.Patches.Setting.setting.js");
            using StreamReader reader = new(stream);
            string jsContent = reader.ReadToEnd();

            jsContent = jsContent.Replace("<<Version>>", MyPluginInfo.PLUGIN_VERSION);

            string jsCode = $"(function() {{ {jsContent} }})();";

            // Lisen for WebView loaded
            wv._webView.WebView.LoadProgressChanged += (sender, args) =>
            {
                if (args.Type == ProgressChangeType.Finished)
                {
                    wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                    {
                        //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                    });
                }
            };

        }
    }
}
