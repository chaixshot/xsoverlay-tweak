using HarmonyLib;
using System.Runtime.CompilerServices;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Haptic
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class GrabHaptic
    {
        private class HoverData
        {
            public bool IsHaptic = true;
        }
        private static readonly ConditionalWeakTable<Raycaster, HoverData> HoverDictionary = new();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Initialize(Raycaster __instance)
        {
            if (!IsHand(__instance)) return;

            HoverDictionary.Add(__instance, new());
        }

        [HarmonyPatch("Grab")]
        [HarmonyPostfix]
        public static void Grab(Raycaster __instance)
        {
            if (!IsEnable()) return;

            if (HoverDictionary.TryGetValue(__instance, out HoverData Data))
            {
                if (__instance.HeldOverlay.IsHeld && Data.IsHaptic)
                {
                    Data.IsHaptic = false;

                    AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.1f, 320f, XConfig.GrabHaptic.Value / 100f);
                }
            }
        }

        [HarmonyPatch("Drop")]
        [HarmonyPrefix]
        public static bool Drop(Raycaster __instance)
        {
            if (!IsEnable()) return true;

            if (HoverDictionary.TryGetValue(__instance, out HoverData Data))
            {
                if (__instance.HeldOverlay == null || Data.IsHaptic)
                {
                    return true;
                }

                Data.IsHaptic = true;
                AdvancedHaptics.Rumble(__instance.HapticDeviceName == Raycaster.HapticDevice.Left, 0.001f, 40f, XConfig.GrabHaptic.Value / 100f);
            }

            return true;
        }

        private static bool IsHand(Raycaster instance)
        {
            return instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }

        private static bool IsEnable()
        {
            return XConfig.GrabHaptic.Value != 0;
        }
    }
}
