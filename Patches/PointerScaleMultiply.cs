using HarmonyLib;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(UI_RelativeTransformManipulator))]
    internal class PointerScaleMultiply
    {
        private static readonly AccessTools.FieldRef<UI_RelativeTransformManipulator, float> ScaleMultiplierRef = AccessTools.FieldRefAccess<UI_RelativeTransformManipulator, float>("scaleMultiplier");

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(UI_RelativeTransformManipulator __instance)
        {
            XConfig.PointerScaleMultiply.SettingChanged += (sender, args) =>
            {
                ScaleMultiplierRef(__instance) = GetScale();
            };
        }

        [HarmonyPatch("OnSetPointerScale")]
        [HarmonyPostfix]
        public static void OnSetPointerScale(ref float ___scaleMultiplier)
        {
            ___scaleMultiplier = GetScale();
        }

        public static float GetScale()
        {
            return XSettingsManager.Instance.Settings.PointerScale * (XConfig.PointerScaleMultiply.Value / 100f);
        }
    }
}
