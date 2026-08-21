using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Mouse;
using xsoverlay_tweak.Patches.Pointer;

namespace xsoverlay_tweak.Utils
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class EventBridge_Raycaster : EventBridge
    {
        public static Unity_Overlay CurrentHoveringOverlay;
        public static Raycaster ActiveRaycaster;
        public static readonly List<Raycaster> Raycaster_List = [];
        public static new bool IsHoverAnyOverlay = false;
        public static new bool IsHoverAnyDesktopOrWindowCapture = false;
        public static new bool IsHoverAnyDesktopCapture = false;
        public static new bool IsHoverAnyWindowCapture = false;
        public static new bool IsHoverAnyWebView = false;

        [HarmonyPatch(typeof(DeviceManager), "Start")]
        [HarmonyPostfix]
        public static void ListenHoveringOverlaySwap()
        {
            XSOEventSystem.OnSwitchHoveringOverlay += async (_raycaster, overlay) =>
            {
                // Reset all hover states before checking the current raycaster's state
                IsHoverAnyOverlay = false;
                IsHoverAnyDesktopOrWindowCapture = false;
                IsHoverAnyDesktopCapture = false;
                IsHoverAnyWindowCapture = false;
                IsHoverAnyWebView = false;

                // Check all raycasters to determine if any are hovering over an overlay
                foreach (Raycaster raycaster in Raycaster_List)
                {
                    IsHoverAnyOverlay = raycaster.HoveringOverlay != null;
                    IsHoverAnyDesktopOrWindowCapture = raycaster.HoveringOverlay?.IsDesktopOrWindowCapture == true;
                    IsHoverAnyDesktopCapture = raycaster.HoveringOverlay?.IsDesktopCapture == true;
                    IsHoverAnyWindowCapture = raycaster.HoveringOverlay?.IsWindowCapture == true;
                    IsHoverAnyWebView = raycaster.HoveringOverlay?.OverlayWebView != null;

                }

                // Update the current hovering overlay based on the active hand and hover state
                if (IsHoverAnyOverlay)
                {
                    await Task.Delay(1); // Wait for 1 millisecond to ensure the hover state is updated
                    if (IsActiveHand(_raycaster))
                        CurrentHoveringOverlay = overlay;
                }
                else
                    CurrentHoveringOverlay = null;
            };
        }

        [HarmonyPatch("HandleClicksForDesktopWindows")]
        [HarmonyPatch("HandleTouchInputForDesktopWindows")]
        [HarmonyPatch("HandleHeadWebAppInput")]
        [HarmonyPatch("BeginWebViewTouch")]
        [HarmonyPatch("BeginWebViewSinglePointer")]
        [HarmonyPatch("RegisterNativeHover")]
        [HarmonyPrefix]
        public static void SwapTargetHand(Raycaster __instance)
        {
            ActiveRaycaster = __instance;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void RaycasterCreated(Raycaster __instance)
        {
            if (__instance != null)
                if (!Raycaster_List.Contains(__instance) && IsRaycasterHand(__instance))
                    Raycaster_List.Add(__instance);
        }

        [HarmonyPatch("HandleScrolling")]
        [HarmonyPrefix]
        public static void ListenScrolling(MouseInputDevice ___InputDevice, Vector2 ___CursorUVNormalized)
        {
            HandleScrolling(___InputDevice.Scroll.axis, ___CursorUVNormalized);
        }

        public static new bool IsActiveHand(Raycaster raycaster, bool skipTwoHanded = false)
        {
            if (PhysicalMouseDetector.IsPhysicalMovement)
                return false;
            else if (TwoHandedMode.IsEnable() && !skipTwoHanded)
                return true;
            else if (DesktopCursorManager.Instance.GetCurrentInputDevice() == raycaster)
                return true;
            else if (IsActiveHandForWebView(raycaster))
                return true;

            return false;
        }

        public static new bool IsActiveHandForWebView(Raycaster raycaster)
        {
            Unity_Overlay overlay = raycaster.HoveringOverlay;

            if (overlay != null)
                if (overlay.OverlayWebView != null && Ref_Raycaster.NativeHoverStates.Contains(overlay))
                {
                    object nativeHoverState = Ref_Raycaster.NativeHoverStates[overlay];

                    if (nativeHoverState != null && Ref_Raycaster.NativeHoverState_Owner(nativeHoverState) == raycaster)
                        return true;
                }

            return false;
        }

        public static new bool IsRaycasterHand(Raycaster raycaster)
        {
            return raycaster.HapticDeviceName != Raycaster.HapticDevice.None;
        }

        public static new Raycaster GetActiveRaycaster()
        {
            return ActiveRaycaster;
        }
    }
}
