using HarmonyLib;
using Valve.VR;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(MouseInputDevice))]

    internal class PullTriggerClickThreshold
    {
        private delegate void MouseClickDelegate(MouseInputDevice instance, bool trigger, bool leftClickState, bool rightClickState, bool middleClickState);
        private static readonly MouseClickDelegate MouseDownInvoke = AccessTools.MethodDelegate<MouseClickDelegate>(AccessTools.Method(typeof(MouseInputDevice), "MouseDown"));
        private static readonly MouseClickDelegate MouseUpInvoke = AccessTools.MethodDelegate<MouseClickDelegate>(AccessTools.Method(typeof(MouseInputDevice), "MouseUp"));

        [HarmonyPatch("DesktopClickHandler")]
        [HarmonyPrefix]
        public static bool IncreaseTriggerClickThreshold(MouseInputDevice __instance)
        {
            bool Trigger = __instance.TriggerDepth.axis >= XConfig.PullTriggerClickThreshold.Value;
            bool LeftClick = MouseInputDevice.LeftClick.GetState(__instance.InputSource);
            bool RightClick = MouseInputDevice.RightClick.GetState(__instance.InputSource) || MouseInputDevice.RightClickGlobal.GetState(SteamVR_Input_Sources.Any);
            bool MiddleClick = MouseInputDevice.MiddleClick.GetState(__instance.InputSource) || MouseInputDevice.MiddleClickGlobal.GetState(SteamVR_Input_Sources.Any);

            if (Trigger || LeftClick || RightClick || MiddleClick)
                MouseDownInvoke(__instance, Trigger, LeftClick, RightClick, MiddleClick);
            else
                MouseUpInvoke(__instance, Trigger, LeftClick, RightClick, MiddleClick);

            return false;
        }
    }
}
