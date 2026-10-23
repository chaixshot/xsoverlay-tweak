using HarmonyLib;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Mouse;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Cursor
{
    internal class AlwaysHideCursor
    {
        private static readonly ConditionalWeakTable<Unity_Overlay, WindowComponentManager> windowManagerCache = new();

        private static bool isPhysicalMovement = false;
        private static readonly ConditionalWeakTable<WindowComponentManager, Coroutine> hideCoroutines = new();
        private static readonly WaitForSecondsRealtime hideDelayWait = new(0.05f);

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void HandlePhysicalMouseDetected()
        {
            PhysicalMouseDetector.mouseDetector.PhysicalMouseMoved += (x, y) =>
            {
                if (!IsEnable()) return;

                if (PhysicalMouseDetector.IsPhysicalMovement && !isPhysicalMovement)
                {
                    isPhysicalMovement = true;
                    BulkSetVisibility(true);
                }
            };

            EventBridge.OnTakeControlOfDesktopCursor += (raycaster) =>
            {
                if (!IsEnable()) return;

                if (isPhysicalMovement)
                {
                    isPhysicalMovement = false;
                    BulkSetVisibility(false);
                }
            };
        }

        [HarmonyPatch(typeof(WindowComponentManager), "CheckShowCursor")]
        [HarmonyPrefix]
        public static bool DisableCheckShowCursor()
        {
            if (!IsEnable() || PhysicalMouseDetector.IsPhysicalMovement) return true;

            return false;
        }

        [HarmonyPatch(typeof(WindowComponentManager), "OnSwitchHoveringOverlay"), HarmonyPatch(typeof(WindowComponentManager), "SetupWindow")]
        [HarmonyPostfix]
        public static void StartHide(WindowComponentManager __instance, Unity_Overlay ___ThisOverlay)
        {
            if (!IsEnable() || PhysicalMouseDetector.IsPhysicalMovement) return;
            if (!EventBridge.IsOverlayDesktopCapture(___ThisOverlay)) return;

            // Avoid overlapping coroutines for the same instance
            if (hideCoroutines.TryGetValue(__instance, out Coroutine coroutine))
            {
                if (coroutine != null)
                {
                    if (Plugin.Instance != null)
                        Plugin.Instance.StopCoroutine(coroutine);
                    else
                        __instance.StopCoroutine(coroutine);
                }
                hideCoroutines.Remove(__instance);
            }

            coroutine = Plugin.Instance != null ? Plugin.Instance.StartCoroutine(HideDelay(__instance)) : __instance.StartCoroutine(HideDelay(__instance));
            hideCoroutines.Add(__instance, coroutine);
        }

        private static IEnumerator HideDelay(WindowComponentManager __instance)
        {
            __instance.ShowCursor(); // SteamVR Dashboard Desktop make cursor reappear

            yield return hideDelayWait;

            __instance.HideCursor();
            hideCoroutines.Remove(__instance);
        }

        private static void BulkSetVisibility(bool visible)
        {
            foreach (Unity_Overlay overlay in Overlay_Manager.Instance.ActiveOverlayComponents)
            {
                WindowComponentManager component = GetWindowManager(overlay);
                if (component != null)
                {
                    if (visible)
                        component.ShowCursor();
                    else
                        component.HideCursor();
                }
            }
        }

        private static WindowComponentManager GetWindowManager(Unity_Overlay overlay)
        {
            if (windowManagerCache.TryGetValue(overlay, out var comp)) return comp;
            comp = overlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>();
            windowManagerCache.Add(overlay, comp);
            return comp;
        }

        private static bool IsEnable()
        {
            return (XConfig.AlwaysHideCursor.Value || (WindowsCursorPointer.IsEnable()) && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse);
        }
    }
}