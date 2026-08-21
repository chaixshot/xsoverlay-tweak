using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using XSOverlay;

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
            bool patchedLerp = false;

            List<CodeInstruction> codes = [.. instructions];

            MethodInfo mathfLerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), [typeof(float), typeof(float), typeof(float)]);
            MethodInfo customSmoothing = AccessTools.Method(typeof(MouseSmoothSpeed), nameof(CalculateCustomSmoothing));

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

                // Replace Mathf.Lerp with custom dynamic smoothing getter
                if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == mathfLerp)
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, customSmoothing);
                    patchedLerp = true;
                }
            }

            if (!patchedAngle || !patchedDistance || !patchedLerp)
                Plugin.Logger.LogError($"PointerSmoothing patch failed (Angle: {patchedAngle}, Distance: {patchedDistance}, Lerp: {patchedLerp}). The mod may be outdated.");

            return codes;
        }

        // Signature updated to accept 3 parameters to match Mathf.Lerp stack consumption
        public static float CalculateCustomSmoothing(float unusedA, float unusedB, float unusedC)
        {
            float currentSetting = Mathf.Clamp01(XSettingsManager.Instance.Settings.PointerSmoothing);

            if (currentSetting <= 0f)
                return 1f;

            // Frame-rate independent weight calculation scaling from subtle (0.1) to heavy smooth (1.0)
            float maxSmoothWeight = Mathf.Clamp01(Time.deltaTime * 1.5f);
            return Mathf.Lerp(1f, maxSmoothWeight, currentSetting);
        }
    }
}