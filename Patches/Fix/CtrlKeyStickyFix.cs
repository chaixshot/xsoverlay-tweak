using HarmonyLib;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class CtrlKeyStickyFix
    {
        [HarmonyPatch(typeof(KeyboardKey), "Start")]
        [HarmonyPostfix]
        public static void CtrlKeySticky(KeyboardKey __instance)
        {
            if (__instance.Label == "ctrl")
            {
                __instance.IsDoubleTappable = IsEnable();

                XConfig.CtrlKeyStickyFix.SettingChanged += (sender, args) =>
                {
                    __instance.IsDoubleTappable = IsEnable();
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.CtrlKeyStickyFix.Value;
        }
    }
}
