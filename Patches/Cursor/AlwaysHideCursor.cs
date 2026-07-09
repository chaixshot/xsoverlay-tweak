using HarmonyLib;
using System.Collections;
using UnityEngine;
using XSOverlay;
using System.Runtime.CompilerServices;
using xsoverlay_tweak.Utils;
using xsoverlay_tweak.Patches.Mouse;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class AlwaysHideCursor
    {
        private static readonly AccessTools.FieldRef<WindowComponentManager, bool> WindowCanShowDesktopCursor = AccessTools.FieldRefAccess<WindowComponentManager, bool>("WindowCanShowDesktopCursor");
        private static readonly ConditionalWeakTable<Unity_Overlay, WindowComponentManager> WindowManagerCache = new();

        private static bool IsPhysicalMovement = false;
        private static readonly ConditionalWeakTable<WindowComponentManager, Coroutine> ActiveHideCoroutines = new();
        private static readonly WaitForSecondsRealtime HideDelayWait = new(0.05f);

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void HandlePhysicalMouseDetected()
        {
            PhysicalMouseDetector.mouseDetector.PhysicalMouseMoved += (x, y) =>
            {
                if (!IsEnable()) return;

                if (PhysicalMouseDetector.IsPhysicalMovement && !IsPhysicalMovement)
                {
                    IsPhysicalMovement = true;

                    var activeOverlays = Overlay_Manager.Instance.ActiveOverlayComponents;
                    for (int i = 0; i < activeOverlays.Count; i++)
                    {
                        Unity_Overlay overlay = activeOverlays[i];
                        if (overlay?.WindowCaptureAPI?.window?.isDesktop == true)
                        {
                            WindowComponentManager component = GetWindowManager(overlay);

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
                            WindowComponentManager component = GetWindowManager(overlay);

                            WindowCanShowDesktopCursor(component) = false;
                        }
                    }
                }
            };
        }

        private static WindowComponentManager GetWindowManager(Unity_Overlay overlay)
        {
            if (WindowManagerCache.TryGetValue(overlay, out var comp)) return comp;
            comp = overlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>();
            WindowManagerCache.Add(overlay, comp);
            return comp;
        }

        [HarmonyPatch(typeof(WindowComponentManager), "OnSwitchHoveringOverlay"), HarmonyPatch(typeof(WindowComponentManager), "SetupWindow")]
        [HarmonyPostfix]
        public static void StartHide(WindowComponentManager __instance, ref bool ___WindowCanShowDesktopCursor)
        {
            if (!IsEnable() || PhysicalMouseDetector.IsPhysicalMovement) return;

            ___WindowCanShowDesktopCursor = true; // SteamVR Dashboard Desktop make cursor reappear

            if (__instance?.WindowAPI?.window?.isDesktop == true) // Disable for Window Capture Mode, Cursor offsetting from Pointer
            {
                // Avoid overlapping coroutines for the same instance
                if (ActiveHideCoroutines.TryGetValue(__instance, out _)) return;
                
                var routine = Plugin.Instance.StartCoroutine(HideDelay(__instance));
                ActiveHideCoroutines.Add(__instance, routine);
            }
        }

        private static IEnumerator HideDelay(WindowComponentManager __instance)
        {
            yield return HideDelayWait;

            WindowCanShowDesktopCursor(__instance) = false;
            ActiveHideCoroutines.Remove(__instance);
        }

        private static bool IsEnable()
        {
            return (XConfig.AlwaysHideCursor.Value || (WindowsCursorPointer.IsEnable()) && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse);
        }
    }
}