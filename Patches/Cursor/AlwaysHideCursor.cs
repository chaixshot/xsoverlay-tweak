using HarmonyLib;
using System.Collections;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class AlwaysHideCursor
    {
        private static readonly AccessTools.FieldRef<WindowComponentManager, bool> WindowCanShowDesktopCursor = AccessTools.FieldRefAccess<WindowComponentManager, bool>("WindowCanShowDesktopCursor");

        private static bool IsPhysicalMovement = false;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void HandlePhysicalMouseDetected()
        {
            PhysicalMouseDetector.mouseDetector.PhysicalMouseMoved += (x, y) =>
            {
                if (!IsEnable()) return;

                if (PhysicalMouseDetector.IsPhysicalMovement)
                {
                    IsPhysicalMovement = true;

                    foreach (Unity_Overlay overlay in Overlay_Manager.Instance.ActiveOverlayComponents)
                    {
                        if (overlay?.WindowCaptureAPI?.window?.isDesktop == true)
                        {
                            WindowComponentManager component = overlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>();

                            WindowCanShowDesktopCursor(component) = true;
                        }
                    }
                }
            };

            EventBridge.OnTakeControlOfDesktopCursor += (raycaster) =>
            {
                if (!IsEnable()) return;

                if (IsPhysicalMovement)
                {
                    IsPhysicalMovement = false;

                    foreach (Unity_Overlay overlay in Overlay_Manager.Instance.ActiveOverlayComponents)
                    {
                        if (overlay?.WindowCaptureAPI?.window?.isDesktop == true)
                        {
                            WindowComponentManager component = overlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>();

                            WindowCanShowDesktopCursor(component) = false;
                        }
                    }
                }
            };
        }

        [HarmonyPatch(typeof(WindowComponentManager), "OnSwitchHoveringOverlay"), HarmonyPatch(typeof(WindowComponentManager), "SetupWindow")]
        [HarmonyPostfix]
        public static void StartHide(WindowComponentManager __instance, ref bool ___WindowCanShowDesktopCursor)
        {
            if (!IsEnable() || PhysicalMouseDetector.IsPhysicalMovement) return;

            ___WindowCanShowDesktopCursor = true; // SteamVR Dashboard Desktop make cursor reappear

            if (__instance?.WindowAPI?.window?.isDesktop == true) // Disable for Window Capture Mode, Cursor offsetting from Pointer
                Plugin.Instance.StartCoroutine(HideDelay(__instance));
        }

        private static IEnumerator HideDelay(WindowComponentManager __instance)
        {
            yield return new WaitForSecondsRealtime(0.05f);

            WindowCanShowDesktopCursor(__instance) = false;
        }

        private static bool IsEnable()
        {
            return (XConfig.AlwaysHideCursor.Value || (WindowsCursorPointer.IsEnable()) && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse);
        }
    }
}