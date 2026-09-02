using HarmonyLib;
using Newtonsoft.Json;
using XSOverlay.Websockets.API;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class StickyKeyHaptic
    {
        [HarmonyPatch(typeof(ApiHandler), "OnSendKeyboardEvent")]
        [HarmonyPostfix]
        public static void PlayHapticOnStickyKey(string jsonData)
        {
            if (!IsEnable()) return;

            Objects.KeyboardEvent keyboardEvent = JsonConvert.DeserializeObject<Objects.KeyboardEvent>(jsonData);

            if (keyboardEvent.keyPressStyle == Enums.KeyPressStyle.Toggle)
                AdvancedHaptics.Rumble(EventBridge.GetActiveKeyboardRaycaster()?.HapticDeviceName == Raycaster.HapticDevice.Left, 0.1f, 320f, 0.5f);
        }

        private static bool IsEnable()
        {
            return XConfig.StickyKeyHaptic.Value;
        }
    }
}
