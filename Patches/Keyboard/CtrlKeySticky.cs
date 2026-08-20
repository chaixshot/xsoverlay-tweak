using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Patches.Keyboard
{
    [HarmonyPatch(typeof(ApiHandler))]
    internal class CtrlKeySticky
    {
        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPrefix]
        public static void ListenCofigChange()
        {
            XConfig.CtrlKeySticky.SettingChanged += (sender, args) =>
            {
                ServerClientBridge.Instance.Api.Commands["OnRequestKeyboardLayout"]("", "", "");
            };
        }

        [HarmonyPatch(typeof(ApiHandler), "OnRequestKeyboardLayout")]
        [HarmonyPrefix]
        public static bool HandleCtrlKeySticky(ApiHandler __instance, string sender)
        {
            KeyboardApiObject keyboardInfo = KeyboardHelper.GetKeyboardLayoutInfo();

            foreach (List<KeyboardKey> keyboardKeys in keyboardInfo.mainKeys)
            {
                foreach (KeyboardKey key in keyboardKeys)
                {
                    if (key.keycode == "LCONTROL" || key.keycode == "RCONTROL")
                        key.isDoubleTappable = IsEnable();
                }
            }

            string data2 = JsonConvert.SerializeObject(keyboardInfo);

            __instance.SendMessage("UpdateKeyboardLayout", data2, null, sender);

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.CtrlKeySticky.Value;
        }
    }
}
