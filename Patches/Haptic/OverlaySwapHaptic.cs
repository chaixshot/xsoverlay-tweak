using HarmonyLib;
using xsoverlay_tweak.Utils;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Haptic
{
    internal class OverlaySwapHaptic
    {
        [HarmonyPatch(typeof(UpdateDateTime)), HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            EventBridge.OnSwitchHoveringOverlay += (raycaster, overlay) =>
            {
                if (!IsEnable()) return;

                if (raycaster.HeldOverlay == null && overlay != null)
                    AdvancedHaptics.Rumble(raycaster.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 320f, XConfig.OverlaySwapHaptic.Value / 100f);
            };
        }

        private static bool IsEnable()
        {
            return XConfig.OverlaySwapHaptic.Value != 0;
        }
    }
}
