using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Pointer
{
    internal class PointerStopSmooth
    {
        [HarmonyPatch(typeof(Raycaster), "CheckOverlayIntersection")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DisableKeyboardSmooth(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            bool patched = false;

            MethodInfo pointerStopSmoothMethod = AccessTools.Method(typeof(PointerStopSmooth), nameof(GetPointerStopSmooth), [typeof(float), typeof(Raycaster)]);

            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i + 3].opcode == OpCodes.Ldc_R4 && (float)codes[i + 3].operand == 0f
                    && (codes[i + 1].opcode == OpCodes.Stloc || codes[i + 1].opcode == OpCodes.Stloc_S
                    || codes[i + 1].opcode == OpCodes.Stloc_1 || codes[i + 1].opcode == OpCodes.Stloc_2 || codes[i + 1].opcode == OpCodes.Stloc_3))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, pointerStopSmoothMethod));

                    patched = true;
                    break;
                }
            }

            if (!patched)
                Plugin.Logger.LogError("[XSOverlay Tweak] PointerStopSmooth transpiler patch for Raycaster.CheckOverlayIntersection failed! Target IL sequence not found.");

            return codes;
        }

        private static float GetPointerStopSmooth(float originalSmoothing, Raycaster instance)
        {
            if (XConfig.PointerStopSmooth.Value == 0 || instance == null)
                return originalSmoothing;

            int mode = XConfig.PointerStopSmooth.Value;
            if (mode == 1 && EventBridge.IsOverlayKeyboard(instance.HoveringOverlay))
                return 0f;

            if (mode == 2 && !EventBridge.IsOverlayDesktpOrWindowCapture(instance.HoveringOverlay))
                return 0f;

            return originalSmoothing;
        }
    }
}
