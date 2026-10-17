using HarmonyLib;
using UnityEngine;
using xsoverlay_tweak.Utils;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Haptic
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class DoubleClickHaptic
    {
        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenDoubleClick()
        {
            Clicker.OnClick += (Raycaster instance, ref Vector2 desktopCoordinate, int button, bool holdingTouch, bool isDoubleClickWin, bool isDoubleClickXSO) =>
            {
                if (!IsEnable()) return;

                if (isDoubleClickWin || isDoubleClickXSO)
                    PlayDoubleClickHaptic();
            };
        }

        private static void PlayDoubleClickHaptic() => AdvancedHaptics.Rumble((DesktopCursorManager.Instance.GetCurrentInputDevice().HapticDeviceName == Raycaster.HapticDevice.Left), 0.05f, 40f, XConfig.DoubleClickHaptic.Value / 100f);
        private static bool IsEnable() => XConfig.DoubleClickHaptic.Value != 0;
    }
}
