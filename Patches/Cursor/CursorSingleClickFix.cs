using HarmonyLib;
using System.Threading.Tasks;
using WindowsInput;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class CursorSingleClickFix
    {
        [HarmonyPatch(typeof(MouseOperations), nameof(MouseOperations.LMouseClick))]
        [HarmonyPrefix]
        public static bool LMouseClick(InputSimulator sim)
        {
            if (!IsEnable()) return true;

            Task.Run(async () =>
            {
                sim.Mouse.LeftButtonDown();
                await Task.Delay(5);
                sim.Mouse.LeftButtonUp();
            });

            return false;
        }

        private static bool IsEnable()
        {
            return XConfig.CursorSingleClickFix.Value;
        }
    }
}