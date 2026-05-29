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

            Dictionary<string, object> settings = new()
            {
                // RefreshRate
                ["XSOverlayTweak.RefreshRate"] = XConfig.RefreshRate.Value,
                ["XSOverlayTweak.OnlyHoverOverlay"] = XConfig.OnlyHoverOverlay.Value,
                ["XSOverlayTweak.OnlyInLayoutMod"] = XConfig.OnlyInLayoutMod.Value,

                // Cursor
                ["XSOverlayTweak.AlwayUpdateCursor"] = XConfig.AlwayUpdateCursor.Value,
                ["XSOverlayTweak.AlwaysHideCursor"] = XConfig.AlwaysHideCursor.Value,
                ["XSOverlayTweak.PhysicalMouseDetector"] = XConfig.PhysicalMouseDetector.Value,
                ["XSOverlayTweak.MouseSmoothSpeed"] = XConfig.MouseSmoothSpeed.Value,

                // Pointer
                ["XSOverlayTweak.ActivePointerWebView"] = XConfig.ActivePointerWebView.Value,
                ["XSOverlayTweak.PointerScaleMultiply"] = XConfig.PointerScaleMultiply.Value,
                ["XSOverlayTweak.PointerDoubleClickDelay"] = XConfig.PointerDoubleClickDelay.Value,
                ["XSOverlayTweak.InactivePointerColor"] = XConfig.InactivePointerColor.Value,
                ["XSOverlayTweak.InactivePointerOpacity"] = XConfig.InactivePointerOpacity.Value,
                ["XSOverlayTweak.WindowsCursorPointer"] = XConfig.WindowsCursorPointer.Value,
                ["XSOverlayTweak.PointerActiveClick"] = XConfig.PointerActiveClick.Value,
                ["XSOverlayTweak.EmulateMouseClickAnimation"] = XConfig.EmulateMouseClickAnimation.Value,

                // Mouse Navigation
                ["XSOverlayTweak.MouseNavigation"] = XConfig.MouseNavigation.Value,
                ["XSOverlayTweak.MouseNavigationUseModifiedKey"] = XConfig.MouseNavigationUseModifiedKey.Value,

                // Dashboard Overlay
                ["XSOverlayTweak.DashboardNotification"] = XConfig.DashboardNotification.Value,
                ["XSOverlayTweak.DashboardPointer"] = XConfig.DashboardPointer.Value,
                ["XSOverlayTweak.DashboardSettings"] = XConfig.DashboardSettings.Value,
                ["XSOverlayTweak.DashboardWindow"] = XConfig.DashboardWindow.Value,
                ["XSOverlayTweak.DashboardWrist"] = XConfig.DashboardWrist.Value,
                ["XSOverlayTweak.Dashboardkeyboard"] = XConfig.Dashboardkeyboard.Value,

                // Optimization
                ["XSOverlayTweak.EfficiencyMode"] = XConfig.EfficiencyMode.Value,
                ["XSOverlayTweak.InactiveRefreshRate"] = XConfig.InactiveRefreshRate.Value,

                // Quality of Life
                ["XSOverlayTweak.DefaultCaptureOverlayTexture"] = XConfig.DefaultCaptureOverlayTexture.Value,
                ["XSOverlayTweak.DoubleClickConfirm"] = XConfig.DoubleClickConfirm.Value,
                ["XSOverlayTweak.LaserPointer"] = XConfig.LaserPointer.Value,
                ["XSOverlayTweak.OverlayCurveAutoRefresh"] = XConfig.OverlayCurveAutoRefresh.Value,
                ["XSOverlayTweak.PinBlockInputNonEditMode"] = XConfig.PinBlockInputNonEditMode.Value,
                ["XSOverlayTweak.PullTriggerClickThreshold"] = XConfig.PullTriggerClickThreshold.Value,
                ["XSOverlayTweak.PullTriggerPointerLock"] = XConfig.PullTriggerPointerLock.Value,
                ["XSOverlayTweak.WebViewWiderScroll"] = XConfig.WebViewWiderScroll.Value,
                ["XSOverlayTweak.WristOverPosition"] = XConfig.WristOverPosition.Value,

                // Fix
                ["XSOverlayTweak.LoadLayoutScaleFix"] = XConfig.LoadLayoutScaleFix.Value,
                ["XSOverlayTweak.OverlayRollCurveFix"] = XConfig.OverlayRollCurveFix.Value,
                ["XSOverlayTweak.WebViewFix"] = XConfig.WebViewFix.Value,

                // About
                ["XSOverlayTweak.UpdateNotification"] = XConfig.UpdateNotification.Value,
            };

            string data = JsonConvert.SerializeObject(settings);
            ServerClientBridge.Instance.Api.SendMessage("UpdateSettings", data, null, sender);
        }

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPrefix]
        public static bool SetSetting(string name, string value, string value1, bool sendAnalytics = true)
        {
            switch (name)
            {
                // RefreshRate
                case "XSOverlayTweak.RefreshRate":
                    XConfig.RefreshRate.Value = RefreshRate.GetFramrate(int.Parse(value));
                    break;
                case "XSOverlayTweak.OnlyHoverOverlay":
                    XConfig.OnlyHoverOverlay.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OnlyInLayoutMod":
                    XConfig.OnlyInLayoutMod.Value = bool.Parse(value);
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
                case "XSOverlayTweak.MouseSmoothSpeed":
                    XConfig.MouseSmoothSpeed.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.WindowsCursorPointer":
                    XConfig.WindowsCursorPointer.Value = bool.Parse(value);
                    break;

                // Pointer
                case "XSOverlayTweak.ActivePointerWebView":
                    XConfig.ActivePointerWebView.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.EmulateMouseClickAnimation":
                    XConfig.EmulateMouseClickAnimation.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.InactivePointerColor":
                    XConfig.InactivePointerColor.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.InactivePointerOpacity":
                    XConfig.InactivePointerOpacity.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.PointerActiveClick":
                    XConfig.PointerActiveClick.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.PointerDoubleClickDelay":
                    XConfig.PointerDoubleClickDelay.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.PointerScaleMultiply":
                    XConfig.PointerScaleMultiply.Value = int.Parse(value);
                    break;

                // Mouse Navigation
                case "XSOverlayTweak.MouseNavigation":
                    XConfig.MouseNavigation.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.MouseNavigationUseModifiedKey":
                    XConfig.MouseNavigationUseModifiedKey.Value = bool.Parse(value);
                    break;

                // Dashboard Overlay
                case "XSOverlayTweak.DashboardNotification":
                    XConfig.DashboardNotification.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.DashboardPointer":
                    XConfig.DashboardPointer.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.DashboardSettings":
                    XConfig.DashboardSettings.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.DashboardWindow":
                    XConfig.DashboardWindow.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.DashboardWrist":
                    XConfig.DashboardWrist.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.Dashboardkeyboard":
                    XConfig.Dashboardkeyboard.Value = bool.Parse(value);
                    break;

                // Optimization
                case "XSOverlayTweak.EfficiencyMode":
                    XConfig.EfficiencyMode.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.InactiveRefreshRate":
                    XConfig.InactiveRefreshRate.Value = int.Parse(value);
                    break;

                // Quality of Life
                case "XSOverlayTweak.DefaultCaptureOverlayTexture":
                    XConfig.DefaultCaptureOverlayTexture.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.DoubleClickConfirm":
                    XConfig.DoubleClickConfirm.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.LaserPointer":
                    XConfig.LaserPointer.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayCurveAutoRefresh":
                    XConfig.OverlayCurveAutoRefresh.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.PinBlockInputNonEditMode":
                    XConfig.PinBlockInputNonEditMode.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.PullTriggerClickThreshold":
                    XConfig.PullTriggerClickThreshold.Value = float.Parse(value);
                    break;
                case "XSOverlayTweak.PullTriggerPointerLock":
                    XConfig.PullTriggerPointerLock.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WebViewWiderScroll":
                    XConfig.WebViewWiderScroll.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WristOverPosition":
                    XConfig.WristOverPosition.Value = bool.Parse(value);
                    break;

                // Fix
                case "XSOverlayTweak.LoadLayoutScaleFix":
                    XConfig.LoadLayoutScaleFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayRollCurveFix":
                    XConfig.OverlayRollCurveFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WebViewFix":
                    XConfig.WebViewFix.Value = bool.Parse(value);
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
            jsContent = jsContent.Replace("<<HMDRefreshRate>>", $"'{DeviceManager.Instance.HMDRefreshRate} FPS'");
            jsContent = jsContent.Replace("<<RefreshRateList>>", string.Join(", ", RefreshRate.RefreshRateList));

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
