using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using XSOverlay.Websockets.API;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class KeyboardPressHaptic : MonoBehaviour
    {
        [HarmonyPatch(typeof(ApiHandler), "OnSendKeyboardEvent")]
        [HarmonyPostfix]
        public static void PlayHapticOnPressButton(string jsonData)
        {
            if (!IsEnable()) return;

            Objects.KeyboardEvent keyboardEvent = JsonConvert.DeserializeObject<Objects.KeyboardEvent>(jsonData);

            if (keyboardEvent.keyPressStyle != Enums.KeyPressStyle.Toggle)
                AdvancedHaptics.Rumble(EventBridge.GetActiveWebViewRaycaster()?.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40, XConfig.KeyboardPressHaptic.Value / 100f);
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardPressHaptic.Value != 0;
        }
    }
}
