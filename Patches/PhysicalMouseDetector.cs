﻿using HarmonyLib;
using System;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class PhysicalMouseDetector
    {
        public static bool IsPhysicalMovement = false;
        private static MouseInputDetector mouseDetector;

        private static readonly Action<Raycaster> TakeControlOverCursorIfNotInControlDelegate = AccessTools.MethodDelegate<Action<Raycaster>>(AccessTools.Method(typeof(Raycaster), "TakeControlOverCursorIfNotInControl"));

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            mouseDetector = new MouseInputDetector();
            mouseDetector.PhysicalMouseMoved += (x, y) =>
            {
                if (IsEnable())
                    IsPhysicalMovement = true;
            };
        }

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("HandleClicksForDesktopWindows")]
        [HarmonyPrefix]
        public static bool HandleClicksForDesktopWindows(Raycaster __instance)
        {
            if (IsPhysicalMovement)
            {
                IsPhysicalMovement = false;
                TakeControlOverCursorIfNotInControlDelegate(__instance);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("HandleTouchInputForDesktopWindows")]
        [HarmonyPrefix]
        public static bool HandleTouchInputForDesktopWindows(Raycaster __instance)
        {
            if (IsPhysicalMovement)
            {
                IsPhysicalMovement = false;
                TakeControlOverCursorIfNotInControlDelegate(__instance);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool HandleTouchInputForWebApplications(Raycaster __instance)
        {
            if (IsPhysicalMovement)
            {
                IsPhysicalMovement = false;
                TakeControlOverCursorIfNotInControlDelegate(__instance);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Raycaster), "SyncedOverlayUpdate")]
        [HarmonyPrefix]
        public static bool SyncedOverlayUpdate()
        {
            return !IsPhysicalMovement;
        }

        private static bool IsEnable()
        {
            return XConfig.PhysicalMouseDetector.Value;
        }
    }
}
