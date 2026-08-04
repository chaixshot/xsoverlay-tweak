using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Wrist
{
    [HarmonyPatch]
    internal class WristOverPosition
    {
        [HarmonyPatch(typeof(Raycaster), "Drop")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RaycasterDrop(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsEnable()) return instructions;

            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.075f)
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.23f);
            }
            return codes;
        }

        [HarmonyPatch(typeof(XSettingsManager), "LoadWristOffsets")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LoadWristOffsets(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsEnable()) return instructions;

            bool patched = false;
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.075f)
                {
                    patched = true;
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.23f);
                }
            }

            if (!patched)
                Plugin.Logger.LogError("WristOverPosition patch failed: Could not find target instruction in XSettingsManager.LoadWristOffsets. The mod may be outdated.");

            return codes;
        }

        private static bool IsEnable()
        {
            return XConfig.WristOverPosition.Value;
        }
    }
}
