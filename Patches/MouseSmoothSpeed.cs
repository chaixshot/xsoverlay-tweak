using HarmonyLib;
using System.Collections.Generic;

namespace xsoverlay_tweak.Patches
{
    internal class MouseSmoothSpeed
    {
        private static readonly List<Raycaster> Instances = [];
        public static List<string> MouseSmoothList = ["'Ultra Low'", "'Very Low'", "'Low'", "'Medium'", "'High'", "'Very High'"];

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void SettingChangingListener()
        {
            XConfig.MouseSmoothSpeed.SettingChanged += (sender, args) =>
            {
                foreach (var __instance in Instances)
                    AccessTools.Field(typeof(Raycaster), "InterpolationSpeed").SetValue(__instance, GetSpeed(XConfig.MouseSmoothSpeed.Value));
            };
        }

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void ApplyMouseSmoothSpeed(Raycaster __instance)
        {
            Instances.Add(__instance);
            AccessTools.Field(typeof(Raycaster), "InterpolationSpeed").SetValue(__instance, GetSpeed(XConfig.MouseSmoothSpeed.Value));
        }

        public static float GetSpeed(string speed)
        {
            return speed switch
            {
                "Ultra Low" => 40f,
                "Very Low" => 20f,
                "Low" => 15f,
                "Medium" => 10f,
                "High" => 5f,
                "Very High" => 0.01f,
                _ => throw new System.NotImplementedException(),
            };
        }

        public static string GetSpeed(int speed)
        {
            return MouseSmoothList[speed].Replace("'", "");
        }
    }
}
