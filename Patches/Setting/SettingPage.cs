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
        public static void WebviewOverlay(OverlayWebView wv)
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
                ["XSOverlayTweak.AlwaysHideCursor"] = XConfig.AlwaysHideCursor.Value,
                ["XSOverlayTweak.AlwaysUpdateCursor"] = XConfig.AlwaysUpdateCursor.Value,
                ["XSOverlayTweak.MouseSmoothSpeed"] = XConfig.MouseSmoothSpeed.Value,
                ["XSOverlayTweak.PhysicalMouseDetector"] = XConfig.PhysicalMouseDetector.Value,
                ["XSOverlayTweak.WindowsCursorPointer"] = XConfig.WindowsCursorPointer.Value,

                // Pointer
                ["XSOverlayTweak.ActivePointerWebView"] = XConfig.ActivePointerWebView.Value,
                ["XSOverlayTweak.PointerScaleMultiply"] = XConfig.PointerScaleMultiply.Value,
                ["XSOverlayTweak.PointerDoubleClickDelay"] = XConfig.PointerDoubleClickDelay.Value,
                ["XSOverlayTweak.InactivePointerColor"] = XConfig.InactivePointerColor.Value,
                ["XSOverlayTweak.InactivePointerOpacity"] = XConfig.InactivePointerOpacity.Value,
                ["XSOverlayTweak.PointerActiveClick"] = XConfig.PointerActiveClick.Value,
                ["XSOverlayTweak.EmulateMouseClickAnimation"] = XConfig.EmulateMouseClickAnimation.Value,

                // Wirst
                ["XSOverlayTweak.fpsVRSocket"] = XConfig.fpsVRSocket.Value,
                ["XSOverlayTweak.WristClipDistance"] = XConfig.WristClipDistance.Value,
                ["XSOverlayTweak.WristOverPosition"] = XConfig.WristOverPosition.Value,
                ["XSOverlayTweak.WristStateRestore"] = XConfig.WristStateRestore.Value,

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

                // Haptic
                ["XSOverlayTweak.GrabHaptic"] = XConfig.GrabHaptic.Value,
                ["XSOverlayTweak.KeyboardKeyHaptic"] = XConfig.KeyboardKeyHaptic.Value,
                ["XSOverlayTweak.KeyboardPressHaptic"] = XConfig.KeyboardPressHaptic.Value,
                ["XSOverlayTweak.OverlaySwapHaptic"] = XConfig.OverlaySwapHaptic.Value,
                ["XSOverlayTweak.StickyKeyHaptic"] = XConfig.StickyKeyHaptic.Value,
                ["XSOverlayTweak.PullTriggerPointerLockHaptic"] = XConfig.PullTriggerPointerLockHaptic.Value,
                ["XSOverlayTweak.ToggleEditModeHaptic"] = XConfig.ToggleEditModeHaptic.Value,
                ["XSOverlayTweak.WebViewHaptic"] = XConfig.WebViewHaptic.Value,

                // Optimization
                ["XSOverlayTweak.EfficiencyMode"] = XConfig.EfficiencyMode.Value,
                ["XSOverlayTweak.InactiveRefreshRate"] = XConfig.InactiveRefreshRate.Value,
                ["XSOverlayTweak.uOSCThreadLoop"] = XConfig.uOSCThreadLoop.Value,

                // Quality of Life
                ["XSOverlayTweak.DefaultCaptureOverlayTexture"] = XConfig.DefaultCaptureOverlayTexture.Value,
                ["XSOverlayTweak.DoubleClickConfirm"] = XConfig.DoubleClickConfirm.Value,
                ["XSOverlayTweak.LaserPointer"] = XConfig.LaserPointer.Value,
                ["XSOverlayTweak.OverlayCurveAutoRefresh"] = XConfig.OverlayCurveAutoRefresh.Value,
                ["XSOverlayTweak.PinBlockInputNonEditMode"] = XConfig.PinBlockInputNonEditMode.Value,
                ["XSOverlayTweak.PullTriggerClickThreshold"] = XConfig.PullTriggerClickThreshold.Value,
                ["XSOverlayTweak.PullTriggerPointerLock"] = XConfig.PullTriggerPointerLock.Value,
                ["XSOverlayTweak.WebViewWiderScroll"] = XConfig.WebViewWiderScroll.Value,
                ["XSOverlayTweak.WindowsAccentColor"] = XConfig.WindowsAccentColor.Value,

                // Fix
                ["XSOverlayTweak.CtrlKeyStickyFix"] = XConfig.CtrlKeyStickyFix.Value,
                ["XSOverlayTweak.CursorMovingInteractionFix"] = XConfig.CursorMovingInteractionFix.Value,
                ["XSOverlayTweak.HandleScrollingFix"] = XConfig.HandleScrollingFix.Value,
                ["XSOverlayTweak.KeyboardControlButtonStateFix"] = XConfig.KeyboardControlButtonStateFix.Value,
                ["XSOverlayTweak.LoadLayoutScaleFix"] = XConfig.LoadLayoutScaleFix.Value,
                ["XSOverlayTweak.OverlayGripSlipFix"] = XConfig.OverlayGripSlipFix.Value,
                ["XSOverlayTweak.OverlayRollCurveFix"] = XConfig.OverlayRollCurveFix.Value,
                ["XSOverlayTweak.WebViewFix"] = XConfig.WebViewFix.Value,

                // Community Request
                ["XSOverlayTweak.HideBattery"] = XConfig.HideBattery.Value,
                ["XSOverlayTweak.HideInvalidBattery"] = XConfig.HideInvalidBattery.Value,
                ["XSOverlayTweak.LoadLayoutKeyboard"] = XConfig.LoadLayoutKeyboard.Value,
                ["XSOverlayTweak.MouseButtonSwap"] = XConfig.MouseButtonSwap.Value,
                ["XSOverlayTweak.OverlayAttachSmooth"] = XConfig.OverlayAttachSmooth.Value,
                ["XSOverlayTweak.OverlayConfirmClose"] = XConfig.OverlayConfirmClose.Value,
                ["XSOverlayTweak.WindowToolbarGesture"] = XConfig.WindowToolbarGesture.Value,
                ["XSOverlayTweak.WindowToolbarKeyboard"] = XConfig.WindowToolbarKeyboard.Value,

                // About
                ["XSOverlayTweak.UpdateNotification"] = XConfig.UpdateNotification.Value,
            };

            string data = JsonConvert.SerializeObject(settings);
            ServerClientBridge.Instance.Api.SendMessage("UpdateSettings", data, null, sender);
        }

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.SetSetting))]
        [HarmonyPrefix]
        public static void SetSetting(string name, string value, string value1, bool sendAnalytics = true)
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
                case "XSOverlayTweak.AlwaysUpdateCursor":
                    XConfig.AlwaysUpdateCursor.Value = bool.Parse(value);
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
                    XConfig.WindowsCursorPointer.Value = int.Parse(value);
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

                // Wrist
                case "XSOverlayTweak.fpsVRSocket":
                    XConfig.fpsVRSocket.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.WristClipDistance":
                    XConfig.WristClipDistance.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.WristOverPosition":
                    XConfig.WristOverPosition.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WristStateRestore":
                    XConfig.WristStateRestore.Value = bool.Parse(value);
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

                // Haptic
                case "XSOverlayTweak.GrabHaptic":
                    XConfig.GrabHaptic.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.KeyboardKeyHaptic":
                    XConfig.KeyboardKeyHaptic.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.KeyboardPressHaptic":
                    XConfig.KeyboardPressHaptic.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.OverlaySwapHaptic":
                    XConfig.OverlaySwapHaptic.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.PullTriggerPointerLockHaptic":
                    XConfig.PullTriggerPointerLockHaptic.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.StickyKeyHaptic":
                    XConfig.StickyKeyHaptic.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.ToggleEditModeHaptic":
                    XConfig.ToggleEditModeHaptic.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WebViewHaptic":
                    XConfig.WebViewHaptic.Value = int.Parse(value);
                    break;

                // Optimization
                case "XSOverlayTweak.EfficiencyMode":
                    XConfig.EfficiencyMode.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.InactiveRefreshRate":
                    XConfig.InactiveRefreshRate.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.uOSCThreadLoop":
                    XConfig.uOSCThreadLoop.Value = bool.Parse(value);
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
                    XConfig.PullTriggerPointerLock.Value = int.Parse(value);
                    break;
                case "XSOverlayTweak.WebViewWiderScroll":
                    XConfig.WebViewWiderScroll.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WindowsAccentColor":
                    XConfig.WindowsAccentColor.Value = bool.Parse(value);
                    break;

                // Fix
                case "XSOverlayTweak.CtrlKeyStickyFix":
                    XConfig.CtrlKeyStickyFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.CursorMovingInteractionFix":
                    XConfig.CursorMovingInteractionFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.HandleScrollingFix":
                    XConfig.HandleScrollingFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.KeyboardControlButtonStateFix":
                    XConfig.KeyboardControlButtonStateFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.LoadLayoutScaleFix":
                    XConfig.LoadLayoutScaleFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayGripSlipFix":
                    XConfig.OverlayGripSlipFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayRollCurveFix":
                    XConfig.OverlayRollCurveFix.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WebViewFix":
                    XConfig.WebViewFix.Value = bool.Parse(value);
                    break;

                // Community Request
                case "XSOverlayTweak.HideBattery":
                    XConfig.HideBattery.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.HideInvalidBattery":
                    XConfig.HideInvalidBattery.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.LoadLayoutKeyboard":
                    XConfig.LoadLayoutKeyboard.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.MouseButtonSwap":
                    XConfig.MouseButtonSwap.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayAttachSmooth":
                    XConfig.OverlayAttachSmooth.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.OverlayConfirmClose":
                    XConfig.OverlayConfirmClose.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WindowToolbarGesture":
                    XConfig.WindowToolbarGesture.Value = bool.Parse(value);
                    break;
                case "XSOverlayTweak.WindowToolbarKeyboard":
                    XConfig.WindowToolbarKeyboard.Value = bool.Parse(value);
                    break;

                // About
                case "XSOverlayTweak.CheckForUpdate":
                    Task.Run(() =>
                    {
                        Utils.Update.CheckForUpdate(true);
                    });
                    break;
                case "XSOverlayTweak.OpenGitHub":
                    Task.Run(Utils.Update.OpenGitHubPage);
                    break;
                case "XSOverlayTweak.UpdateNotification":
                    XConfig.UpdateNotification.Value = bool.Parse(value);
                    break;
            }
        }

        public static void InjectSettingsModule(OverlayWebView wv)
        {

            // JS for inserting the actual settings page
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("xsoverlay_tweak.Patches.Setting.setting.js");
            using StreamReader reader = new(stream);
            string jsContent = reader.ReadToEnd();

            jsContent = jsContent.Replace("<<Version>>", MyPluginInfo.PLUGIN_VERSION);
            jsContent = jsContent.Replace("<<HMDRefreshRate>>", DeviceManager.Instance.HMDRefreshRate.ToString());
            jsContent = jsContent.Replace("<<RefreshRate>>", XConfig.RefreshRate.Value);
            jsContent = jsContent.Replace("<<RefreshRateList>>", string.Join(", ", RefreshRate.RefreshRateList));

            string jsCode = $"(function() {{ {jsContent} }})();";

            // Lisen for WebView loaded
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
}
