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
        public static IEnumerable<CodeInstruction> Increase_RaycasterDrop_Dist(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsEnable()) return instructions;

            bool patched = false;
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.075f)
                {
                    patched = true;
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.23f);
                }
            }

            if (!patched)
                Plugin.Logger.LogError("WristOverPosition patch failed: Could not find target instruction in Raycaster.Drop. The mod may be outdated.");

            return codes;
        }

        [HarmonyPatch(typeof(XSettingsManager), "LoadWristOffsets")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Increase_LoadWristOffsets_Dist(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsEnable()) return instructions;

            bool patched = false;
            List<CodeInstruction> codes = [.. instructions];
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

        [HarmonyPatch(typeof(XSettingsManager), "StoreWristOffsets")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Increase_StoreWristOffsets_Dist(IEnumerable<CodeInstruction> instructions)
        {
            if (!IsEnable()) return instructions;

            bool patched = false;
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.1f)
                {
                    patched = true;
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0.23f);
                }
            }

            if (!patched)
                Plugin.Logger.LogError("WristOverPosition patch failed: Could not find target instruction in XSettingsManager.StoreWristOffsets. The mod may be outdated.");

            return codes;
        }

        private static bool IsEnable()
        {
            return XConfig.WristOverPosition.Value;
        }
    }
}
