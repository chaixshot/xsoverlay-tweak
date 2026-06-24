using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    internal class SteamVRCompositorTextureFormat
    {
        private const uint DXGI_FORMAT_R8G8B8A8_UNORM = 28;
        private const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;

        [HarmonyPatch(typeof(GetOverlayTexture), "Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UseNativeTextureFormat(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);

            if (!IsEnable()) return codes;

            MethodInfo getOverlayTexture = AccessTools.Method(typeof(Valve.VR.CVROverlay), nameof(Valve.VR.CVROverlay.GetOverlayTexture));
            MethodInfo createExternalTexture = AccessTools.Method(
                typeof(Texture2D),
                nameof(Texture2D.CreateExternalTexture),
                new[] { typeof(int), typeof(int), typeof(TextureFormat), typeof(bool), typeof(bool), typeof(System.IntPtr) });

            object nativeFormatLocal = null;

            for (int i = 0; i < codes.Count; i++)
            {
                if (i >= 4 && codes[i].Calls(getOverlayTexture) && LoadsLocalAddress(codes[i - 4]))
                {
                    nativeFormatLocal = codes[i - 4].operand;
                    break;
                }
            }

            if (nativeFormatLocal == null)
            {
                Plugin.Logger.LogWarning("SteamVRCompositorTextureFormat: GetOverlayTexture native format local was not found; leaving method unchanged.");
                return codes;
            }

            int patchedCalls = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(createExternalTexture))
                    continue;

                int formatIndex = i - 4;
                if (formatIndex < 0 || !LoadsTextureFormat(codes[formatIndex], TextureFormat.BGRA32))
                {
                    Plugin.Logger.LogWarning("SteamVRCompositorTextureFormat: CreateExternalTexture format argument did not match expected BGRA32 constant; skipping call.");
                    continue;
                }

                CodeInstruction originalFormatInstruction = codes[formatIndex];

                codes[formatIndex] = new CodeInstruction(OpCodes.Ldloc_S, nativeFormatLocal);
                codes[formatIndex].labels.AddRange(originalFormatInstruction.labels);
                codes[formatIndex].blocks.AddRange(originalFormatInstruction.blocks);
                codes.InsertRange(formatIndex + 1, new[]
                {
                    new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)TextureFormat.BGRA32),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SteamVRCompositorTextureFormat), nameof(GetSteamVRTextureFormat)))
                });
                patchedCalls++;
                i += 2;
            }

            if (patchedCalls == 0)
                Plugin.Logger.LogWarning("SteamVRCompositorTextureFormat: no CreateExternalTexture calls were patched.");

            return codes;
        }

        private static TextureFormat GetSteamVRTextureFormat(uint nativeFormat, TextureFormat fallback)
        {
            return nativeFormat switch
            {
                DXGI_FORMAT_B8G8R8A8_UNORM => TextureFormat.BGRA32,
                DXGI_FORMAT_R8G8B8A8_UNORM => TextureFormat.RGBA32,
                _ => fallback
            };
        }

        private static bool LoadsLocalAddress(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S;
        }

        private static bool LoadsTextureFormat(CodeInstruction instruction, TextureFormat format)
        {
            int expected = (int)format;

            if (instruction.opcode == OpCodes.Ldc_I4)
                return instruction.operand is int value && value == expected;

            if (instruction.opcode == OpCodes.Ldc_I4_S)
                return instruction.operand is sbyte shortValue && shortValue == expected;

            return instruction.opcode == OpCodes.Ldc_I4_0 && expected == 0
                || instruction.opcode == OpCodes.Ldc_I4_1 && expected == 1
                || instruction.opcode == OpCodes.Ldc_I4_2 && expected == 2
                || instruction.opcode == OpCodes.Ldc_I4_3 && expected == 3
                || instruction.opcode == OpCodes.Ldc_I4_4 && expected == 4
                || instruction.opcode == OpCodes.Ldc_I4_5 && expected == 5
                || instruction.opcode == OpCodes.Ldc_I4_6 && expected == 6
                || instruction.opcode == OpCodes.Ldc_I4_7 && expected == 7
                || instruction.opcode == OpCodes.Ldc_I4_8 && expected == 8;
        }

        private static bool IsEnable()
        {
            return XConfig.SteamVRCompositorTextureFormat.Value;
        }
    }
}
