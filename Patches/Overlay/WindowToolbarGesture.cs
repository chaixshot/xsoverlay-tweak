using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using uWindowCapture;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class WindowToolbarGesture
    {
        private class WindowData
        {
            public UwcWindow LastWindow;
            public int ScrollIndex = 0;
            public float NextScrollTime;
        }
        private static readonly ConditionalWeakTable<Unity_Overlay, WindowData> LastWindow = new();

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ScrollWindowToolbarToChangeWindow()
        {
            EventBridge.OnHandleScrolling += (ScrollAxis, normalizedPoint) =>
            {
                if (!IsEnable()) return;

                if (EventBridge.CurrentHoveringOverlay?.overlayName == "window.toolbar")
                {
                    Unity_Overlay targetOverlay = Overlay_Manager.Instance.WindowToolbarMover.ParentOverlay;

                    if (LastWindow.TryGetValue(targetOverlay, out WindowData Data))
                    {
                        if (Math.Abs(ScrollAxis.y) > 0.3f)
                        {
                            if (Time.time >= Data.NextScrollTime)
                            {
                                Data.NextScrollTime = Time.time + 0.5f;

                                WindowComponentManager windowComponent = targetOverlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>(true);
                                Dictionary<int, UwcWindowList.WindowListItem?> currentWindowList = UwcWindowList.Instance.CurrentWindowList;
                                int maxWindow = currentWindowList.Count;
                                int scrollIndex = Data.ScrollIndex;

                                if (maxWindow == 0) return;

                            startover:
                                if (ScrollAxis.y < 0)
                                    scrollIndex--;
                                else
                                    scrollIndex++;

                                if (scrollIndex >= maxWindow)
                                    scrollIndex = 0;
                                else if (scrollIndex < 0)
                                    scrollIndex = maxWindow - 1;

                                UwcWindowList.WindowListItem? targetItem = Enumerable.ElementAtOrDefault(currentWindowList.Values, scrollIndex);
                                if (targetItem == null) return;
                                int windowTargetId = targetItem.Value.id;

                                if (windowTargetId == windowComponent.WindowAPI.window.id) // Same window
                                    goto startover;

                                windowComponent?.SetOverlayCaptureTarget(targetOverlay, windowTargetId);
                            }
                        }
                        else
                            Data.NextScrollTime = 0f;
                    }
                }
            };
        }

        [HarmonyPatch(typeof(Raycaster), "HandleTouchInputForWebApplications")]
        [HarmonyPrefix]
        public static bool RightClickWindowToolbar(Raycaster __instance, ClickActions clickActions)
        {
            if (!IsEnable()) return true;

            if (EventBridge.CurrentHoveringOverlay?.overlayName == "window.toolbar")
                if (EventBridge.IsActiveHand(__instance) && clickActions.ActionIndex == 1)
                {
                    Unity_Overlay targetOverlay = Overlay_Manager.Instance.WindowToolbarMover.ParentOverlay;

                    if (LastWindow.TryGetValue(targetOverlay, out WindowData Data))
                    {
                        WindowComponentManager windowComponent = targetOverlay.overlayRootObject.GetComponentInChildren<WindowComponentManager>(true);
                        windowComponent.SetOverlayCaptureTarget(targetOverlay, Data.LastWindow);

                        return false;
                    }
                }

            return true;
        }

        [HarmonyPatch(typeof(WindowComponentManager), nameof(WindowComponentManager.SetOverlayCaptureTarget), [typeof(Unity_Overlay), typeof(int)]), HarmonyPatch(typeof(WindowComponentManager), nameof(WindowComponentManager.SetOverlayCaptureTarget), [typeof(Unity_Overlay), typeof(UwcWindow)])]
        [HarmonyPrefix]
        public static void RememberCaptureTarget(WindowComponentManager __instance, Unity_Overlay overlay, Unity_Overlay ___ThisOverlay)
        {
            if (!IsEnable()) return;

            if (!(___ThisOverlay != overlay))
            {
                if (LastWindow.TryGetValue(overlay, out WindowData Data))
                    Data.LastWindow = __instance.WindowAPI.window;
                else
                {
                    LastWindow.Add(overlay, new WindowData
                    {
                        LastWindow = __instance.WindowAPI.window,
                    });
                }
            }
        }

        [HarmonyPatch(typeof(WindowComponentManager), nameof(WindowComponentManager.SetOverlayCaptureTarget), [typeof(Unity_Overlay), typeof(int)]), HarmonyPatch(typeof(WindowComponentManager), nameof(WindowComponentManager.SetOverlayCaptureTarget), [typeof(Unity_Overlay), typeof(UwcWindow)])]
        [HarmonyPostfix]
        public static void SetScrollIndexFromSelectedWindow(WindowComponentManager __instance, Unity_Overlay overlay, Unity_Overlay ___ThisOverlay)
        {
            if (!IsEnable()) return;

            if (!(___ThisOverlay != overlay))
            {
                Dictionary<int, UwcWindowList.WindowListItem?> currentWindowList = UwcWindowList.Instance.CurrentWindowList;
                int windowTargetId = __instance.WindowAPI.window?.id ?? 0;
                int scrollIndex = Enumerable.TakeWhile(currentWindowList.Values, item => item == null || item.Value.id != windowTargetId).Count();

                if (LastWindow.TryGetValue(overlay, out WindowData Data))
                    Data.ScrollIndex = scrollIndex;
                else
                {
                    LastWindow.Add(overlay, new WindowData
                    {
                        LastWindow = __instance.WindowAPI.window,
                        ScrollIndex = scrollIndex
                    });
                }
            }
        }

        private static bool IsEnable()
        {
            return XConfig.WindowToolbarGesture.Value;
        }
    }
}
