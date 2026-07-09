using HarmonyLib;

namespace xsoverlay_tweak.Patches.Keyboard
{
    internal class CtrlKeySticky
    {
        [HarmonyPatch(typeof(KeyboardKey), "Start")]
        [HarmonyPostfix]
        public static void AddCtrlKeySticky(KeyboardKey __instance)
        {
            if (__instance.Label == "ctrl")
            {
                __instance.IsDoubleTappable = IsEnable();

                XConfig.CtrlKeySticky.SettingChanged += (sender, args) =>
                {
                    __instance.IsDoubleTappable = IsEnable();
                };
            }
        }

        private static bool IsEnable()
        {
            return XConfig.CtrlKeySticky.Value;
        }
    }
}
