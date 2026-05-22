using HarmonyLib;
using System.Collections.Generic;

namespace xsoverlay_tweak.Patches
{
    internal class AlwaysHideCursor
    {
        // Static list to track all active window managers
        private static readonly List<WindowComponentManager> instanceRefs = [];

        // Cache the private field once for high-speed access
        private static readonly AccessTools.FieldRef<WindowComponentManager, bool> WindowCursorRef = AccessTools.FieldRefAccess<WindowComponentManager, bool>("WindowCanShowDesktopCursor");

        [HarmonyPatch(typeof(WindowComponentManager), "Start")]
        [HarmonyPostfix]
        public static void Start(WindowComponentManager __instance)
        {
            if (!instanceRefs.Contains(__instance))
                instanceRefs.Add(__instance);
        }

        [HarmonyPatch(typeof(WindowComponentManager), "OnSwitchHoveringOverlay")]
        [HarmonyPostfix]
        public static void OnSwitchHoveringOverlay()
        {
            if (!IsEnable()) return;

            // Loop backwards through all managers
            for (int i = instanceRefs.Count - 1; i >= 0; i--)
            {
                WindowComponentManager manager = instanceRefs[i];

                // If window was destroyed, remove from list and skip
                if (manager == null)
                {
                    instanceRefs.RemoveAt(i);
                    continue;
                }

                // Set the private boolean to false for EVERY manager
                WindowCursorRef(manager) = false;
            }
        }

        private static bool IsEnable()
        {
            return XConfig.AlwaysHideCursor.Value || XConfig.WindowsCursorPointer.Value;
        }
    }
}