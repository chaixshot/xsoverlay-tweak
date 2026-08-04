using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(MouseInputDevice))]
    internal class PullTriggerClickThreshold
    {
        [HarmonyPatch("DesktopClickHandler")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeDesktopClickHandlerThreshold(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            List<CodeInstruction> codes = new(instructions);
            FieldInfo pullTriggerClickThreshold = AccessTools.Field(typeof(XConfig), nameof(XConfig.PullTriggerClickThreshold));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.3f)
                {
                    patched = true;
                    codes[i] = new CodeInstruction(OpCodes.Ldsfld, pullTriggerClickThreshold);
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(pullTriggerClickThreshold.FieldType, "Value")));
                    break;
                }
            }
            
            if (!patched)
                Plugin.Logger.LogError("PullTriggerClickThreshold patch failed: Could not find target instruction in MouseInputDevice.DesktopClickHandler. The mod may be outdated.");

            return codes;
        }
    }
}
