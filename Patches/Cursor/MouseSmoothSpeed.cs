using HarmonyLib;
using System.Collections.Generic;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class MouseSmoothSpeed
    {
        private static readonly List<Raycaster> Instances = [];

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void SettingChangingListener()
        {
            XConfig.MouseSmoothSpeed.SettingChanged += (sender, args) =>
            {
                foreach (var __instance in Instances)
                    AppylySmoothSpeed(__instance);
            };
        }

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void ApplyMouseSmoothSpeed(Raycaster __instance)
        {
            Instances.Add(__instance);
            AppylySmoothSpeed(__instance);
        }

        public static void AppylySmoothSpeed(Raycaster raycaster)
        {
            EventBridge.Ref_Raycaster.InterpolationSpeed(raycaster) = GetSpeed(XConfig.MouseSmoothSpeed.Value);

        }

        public static float GetSpeed(int speed)
        {
            return speed switch
            {
                0 => 40f,
                1 => 20f,
                2 => 15f,
                3 => 10f,
                4 => 5f,
                5 => 0.01f,
                _ => throw new System.NotImplementedException(),
            };
        }
    }
}
