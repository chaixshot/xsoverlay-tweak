using HarmonyLib;
using System;
using Valve.Newtonsoft.Json;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Utils
{
    internal class CustomAPI
    {
        public static event Action<bool, bool> OnToggleMediaPlayer;
        public static event Action<XSONotificationObject> OnShowNotification;

        [Serializable]
        public class XSONotificationObject
        {
            public int type;
            public int index; // Deprecated but used for media player
            public float timeout;
            public float height;
            public float width;
            public float opacity;
            public float volume;
            public string audioPath;
            public string title;
            public string content;
            public bool useBase64Icon;
            public string icon;
            public string sourceApp;
        }

        // Payload DTO matching the JS JSON object
        public class ToggleMediaPlayerPayload
        {
            public bool ShowMediaPlayer { get; set; }
            public bool byclick { get; set; }
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void InjectWristCustomAPI(OverlayWebView wv)
        {
            // Wrist
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Wrist)
            {
                wv.WebViewReady += (IWebView) =>
                {
                    string jsCode = @"
                        (function hookWristMediaEvents() {
                            if (window.toolbarContext && typeof window.toolbarContext.onToggleMediaPlayer === 'function') {
                                const ctx = window.toolbarContext;

                                // Helper function to send the combined JSON object payload
                                const sendMediaState = (byClick) => {
                                    try {
                                        const isShown = ctx.state?.ShowMediaPlayer;
                                        const payload = JSON.stringify({
                                            ShowMediaPlayer: !!isShown,
                                            byClick: !!byClick
                                        });
                                        ctx.api.Send('Tweak_ToggleMediaPlayer', payload, null);
                                    } catch (e) {
                                        console.error('[Tweak Wrist Hook Error]', e);
                                    }
                                };

                                // 1Hook Manual Toggles (Buttons / User Input -> byClick: true)
                                if (!ctx.onToggleMediaPlayer._tweakHooked) {
                                    const originalToggle = ctx.onToggleMediaPlayer;
                                    ctx.onToggleMediaPlayer = function(override) {
                                        originalToggle.apply(this, arguments);
                                        sendMediaState(true);
                                    };
                                    ctx.onToggleMediaPlayer._tweakHooked = true;
                                }

                                // Hook Automatic Music Detection (Auto-Open on Music Play -> byClick: false)
                                if (typeof ctx.onThemeMediaPlayer === 'function' && !ctx.onThemeMediaPlayer._tweakHooked) {
                                    const originalTheme = ctx.onThemeMediaPlayer;
                                    ctx.onThemeMediaPlayer = function(useMediaTheme, sendApiEvent) {
                                        const wasShown = ctx.state?.ShowMediaPlayer;
                                        originalTheme.apply(this, arguments);
                                        const isNowShown = ctx.state?.ShowMediaPlayer;

                                        // Send if state changed or if auto-detected media opened the player
                                        if (wasShown !== isNowShown || (useMediaTheme && isNowShown)) {
                                            sendMediaState(false);
                                        }
                                    };
                                    ctx.onThemeMediaPlayer._tweakHooked = true;
                                }

                                console.log('[Tweak] Manual and Auto-Music hooks attached.');
                            } else {
                                setTimeout(hookWristMediaEvents, 50);
                            }
                        })();
                    ";

                    wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                    {
                        //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                    });
                };
            }

            // Notification
            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.Notification)
            {
                wv.WebViewReady += (IWebView) =>
                {
                    string jsCode = @"
                        (function hookNotificationQueue() {
                            if (window.context?.state?.NotificationQueue) {
                                const queue = window.context.state.NotificationQueue;
                    
                                if (!queue._tweakHooked) {
                                    const originalPush = queue.push;
                                    queue.push = function(...items) {
                                        items.forEach(notification => {
                                            try {
                                                const payload = typeof notification === 'string' 
                                                    ? notification 
                                                    : JSON.stringify(notification);

                                                window.context.Api.Send('Tweak_ShowNotification', payload, null);
                                            } catch (e) {
                                                console.error('[Tweak Queue Hook Error]', e);
                                            }
                                        });
                                        return originalPush.apply(this, items);
                                    };
                                    queue._tweakHooked = true;
                                }
                            } else {
                                setTimeout(hookNotificationQueue, 50);
                            }
                        })();
                    ";

                    wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                    {
                        //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                    });
                };
            }
        }

        [HarmonyPatch(typeof(ApiHandler), "InitializeAPI")]
        [HarmonyPostfix]
        public static void AddCustomAPI(ApiHandler __instance)
        {
            // Call after wrist media player toggled
            __instance.Commands.Add("Tweak_ToggleMediaPlayer", delegate (string sender, string jsonData, string data)
            {
                ToggleMediaPlayerPayload payload = JsonConvert.DeserializeObject<ToggleMediaPlayerPayload>(jsonData);
                OnToggleMediaPlayer?.Invoke(payload.ShowMediaPlayer, payload.byclick);
            });

            // Call after the notification is shown, including shows from the queue.
            __instance.Commands.Add("Tweak_ShowNotification", delegate (string sender, string jsonData, string data)
            {
                XSONotificationObject notification = JsonConvert.DeserializeObject<XSONotificationObject>(jsonData);
                OnShowNotification?.Invoke(notification);
            });
        }
    }
}
