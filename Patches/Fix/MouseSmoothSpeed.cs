using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class MouseSmoothSpeed
    {
        [HarmonyPatch(typeof(Raycaster), "CheckOverlayIntersection")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixBetaSmooth(IEnumerable<CodeInstruction> instructions)
        {
            bool patchedAngle = false;
            bool patchedDistance = false;

            List<CodeInstruction> codes = [.. instructions];

            for (int i = 0; i < codes.Count; i++)
            {
                // Remove strict reset angle threshold (0.1f -> 1000f)
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.1f)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 1000f);
                    patchedAngle = true;
                }

                // Remove strict reset distance threshold (0.001f -> 1000f)
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.001f)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 1000f);
                    patchedDistance = true;
                }
            }

            if (!patchedAngle || !patchedDistance)
                Plugin.Logger.LogError($"PointerSmoothing patch failed (Angle: {patchedAngle}, Distance: {patchedDistance}). The mod may be outdated.");

            return codes;
        }
    }
}