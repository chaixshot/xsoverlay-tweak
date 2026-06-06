using HarmonyLib;
using System.Collections;
using UnityEngine;
using Valve.VR;
using XSOverlay;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    internal class fpsVRSocket
    {
        private static CVROverlay Overlay;
        private static EVROverlayError error;
        private static ulong fpsVRHandle = 0;

        private static bool IsClosing = false;
        private static bool IsPerformanceMonitor = false;
        private static Coroutine ClosingCoroutine;

        [HarmonyPatch(typeof(DeviceManager), "Awake")]
        [HarmonyPostfix]
        public static void Awake(DeviceManager __instance)
        {
            XSOEventSystem.OnToggleLayoutMode += (enable) =>
            {
                if (!IsEnabled()) return;

                if (XConfig.fpsVRSocket.Value == 1) // Top
                {
                    if (ClosingCoroutine != null)
                        Plugin.Instance.StopCoroutine(ClosingCoroutine);

                    if (!enable)
                    {
                        IsClosing = true;
                        ClosingCoroutine = Plugin.Instance.StartCoroutine(ClosingDelay());
                    }
                    else
                        ChangefpsVRTranform();
                }
            };

            XSOEventSystem.OnStartStopPerformanceMonitor += (enable) =>
            {
                if (!IsEnabled()) return;

                IsPerformanceMonitor = enable;

                if (XConfig.fpsVRSocket.Value == 1) // Top
                {
                    if (ClosingCoroutine != null)
                        Plugin.Instance.StopCoroutine(ClosingCoroutine);

                    if (!enable)
                    {
                        IsClosing = true;
                        ClosingCoroutine = Plugin.Instance.StartCoroutine(ClosingDelay());
                    }
                    else
                        ChangefpsVRTranform();
                }
            };

            XConfig.fpsVRSocket.SettingChanged += (sender, args) =>
            {
                if (!IsEnabled()) return;

                ChangefpsVRTranform();
            };
        }

        [HarmonyPatch(typeof(DeviceManager), "UpdateDevices")]
        [HarmonyPostfix]
        public static void UpdateInTick()
        {
            if (!IsEnabled()) return;

            if (fpsVRHandle == 0)
            {
                Overlay = OpenVR.Overlay;
                EVROverlayError evroverlayError = Overlay.FindOverlay("sb.fpsVR.ingameoverlay", ref fpsVRHandle);

                if (evroverlayError != EVROverlayError.None)
                    Plugin.Logger.LogError(evroverlayError.ToString());
            }

            if (!IsClosing)
                ChangefpsVRTranform();
        }

        [HarmonyPatch(typeof(WindowMovementManager), "DoScaleWindowFixed")]
        [HarmonyPostfix]
        public static void UpdateOnScaling(ref Unity_Overlay activeOverlay)
        {
            if (!IsEnabled()) return;

            if (WristOverlay.Instance.overlay == activeOverlay)
                ChangefpsVRTranform();
        }

        [HarmonyPatch(typeof(WindowMovementManager), "DetermineIfOverlayShouldBeCurved")]
        [HarmonyPostfix]
        public static void UpdateOnMoving(ref Unity_Overlay overlay)
        {
            if (!IsEnabled()) return;

            if (WristOverlay.Instance.overlay == overlay)
                ChangefpsVRTranform();
        }

        [HarmonyPatch(typeof(Raycaster), "Drop")]
        [HarmonyPostfix]
        public static void UpdateOnPostMoved()
        {
            if (!IsEnabled()) return;

            ChangefpsVRTranform();
        }

        private static void ChangefpsVRTranform()
        {
            if (fpsVRHandle == 0 || WristOverlay.Instance == null) return;

            // Get the real runtime physical sizes directly from the active XSOverlay instance metrics
            float xsoWidth = WristOverlay.Instance.overlay.overlay.overlayTexture.width;
            float xsoHeight = WristOverlay.Instance.overlay.overlay.overlayTexture.height;
            float xsoWidthInMeters = WristOverlay.Instance.overlay.overlay.overlayWidthInMeters;
            float xsoHeightInMeters = xsoWidthInMeters * ((float)xsoHeight / xsoWidth);

            uint fpsWidth = 0;
            uint fpsHeight = 0;
            Overlay.GetOverlayTextureSize(fpsVRHandle, ref fpsWidth, ref fpsHeight);
            float fpsWidthInMeters = 0f;
            Overlay.GetOverlayWidthInMeters(fpsVRHandle, ref fpsWidthInMeters);// Fetch the native tracking target widths from the external fpsVR application layout
            float fpsHeightInMeters = fpsWidthInMeters * ((float)fpsHeight / fpsWidth); // Extrapolate fpsVR Height using its real rendering aspect ratio context

            VROverlayTransformType transformType = WristOverlay.Instance.overlay.overlay.overlayTransformType;
            HmdMatrix34_t overlayTransform = WristOverlay.Instance.overlay.overlay.overlayTransform;

            // Calculate alignment steps
            if (XConfig.fpsVRSocket.Value == 1) // Top
            {
                float yOffset = (xsoHeightInMeters / 2f) + (fpsHeightInMeters / 2f);

                if (Overlay_Manager.Instance.editMode)
                    yOffset += (xsoHeightInMeters * -0.05f);
                else
                {
                    if (IsPerformanceMonitor)
                        yOffset += (xsoHeightInMeters * -0.2f);
                    else
                        yOffset += (xsoHeightInMeters * -0.3f);
                }


                overlayTransform = AddOffset(overlayTransform, new Vector3(0f, yOffset, 0f), Quaternion.identity);
            }
            else if (XConfig.fpsVRSocket.Value == 2) // Bottom
            {
                float yOffset = -((xsoHeightInMeters / 2f) + (fpsHeightInMeters / 2f));

                yOffset += (xsoHeightInMeters * +0.3f);

                overlayTransform = AddOffset(overlayTransform, new Vector3(0f, yOffset, 0f), Quaternion.identity);
            }
            else if (XConfig.fpsVRSocket.Value == 3) // Left
            {
                float xOffset = -((xsoWidthInMeters / 2f) + (fpsWidthInMeters / 2f));
                overlayTransform = AddOffset(overlayTransform, new Vector3(xOffset, 0f, 0f), Quaternion.identity);
            }
            else if (XConfig.fpsVRSocket.Value == 4) // Right
            {
                float xOffset = (xsoWidthInMeters / 2f) + (fpsWidthInMeters / 2f);
                overlayTransform = AddOffset(overlayTransform, new Vector3(xOffset, 0f, 0f), Quaternion.identity);
            }

            if (transformType == VROverlayTransformType.VROverlayTransform_Absolute || transformType != VROverlayTransformType.VROverlayTransform_TrackedDeviceRelative) // Floating
            {
                //ETrackingUniverseOrigin overlayTransformAbsoluteTrackingOrigin = WristOverlay.Instance.overlay.overlay.overlayTransformAbsoluteTrackingOrigin;
                //error = Overlay.SetOverlayTransformAbsolute(fpsVRHandle, overlayTransformAbsoluteTrackingOrigin, ref overlayTransform);
            }
            else // Apply tracking changes to the target handle destination
            {
                uint overlayTransformTrackedDeviceRelativeIndex = WristOverlay.Instance.overlay.overlay.overlayTransformTrackedDeviceRelativeIndex;
                error = Overlay.SetOverlayTransformTrackedDeviceRelative(fpsVRHandle, overlayTransformTrackedDeviceRelativeIndex, ref overlayTransform);
            }
        }

        private static IEnumerator ClosingDelay()
        {
            yield return new WaitForSeconds(0.4f);

            ChangefpsVRTranform();
            ClosingCoroutine = null;
            IsClosing = false;
        }

        private static HmdMatrix34_t AddOffset(HmdMatrix34_t a, Vector3 posOffset, Quaternion rotOffset)
        {
            Matrix4x4 bMat = Matrix4x4.TRS(posOffset, rotOffset, Vector3.one);

            HmdMatrix34_t result = new()
            {
                // Row 0
                m0 = (a.m0 * bMat.m00) + (a.m1 * bMat.m10) + (a.m2 * bMat.m20),
                m1 = (a.m0 * bMat.m01) + (a.m1 * bMat.m11) + (a.m2 * bMat.m21),
                m2 = (a.m0 * bMat.m02) + (a.m1 * bMat.m12) + (a.m2 * bMat.m22),
                m3 = (a.m0 * bMat.m03) + (a.m1 * bMat.m13) + (a.m2 * bMat.m23) + a.m3,

                // Row 1
                m4 = (a.m4 * bMat.m00) + (a.m5 * bMat.m10) + (a.m6 * bMat.m20),
                m5 = (a.m4 * bMat.m01) + (a.m5 * bMat.m11) + (a.m6 * bMat.m21),
                m6 = (a.m4 * bMat.m02) + (a.m5 * bMat.m12) + (a.m6 * bMat.m22),
                m7 = (a.m4 * bMat.m03) + (a.m5 * bMat.m13) + (a.m6 * bMat.m23) + a.m7,

                // Row 2
                m8 = (a.m8 * bMat.m00) + (a.m9 * bMat.m10) + (a.m10 * bMat.m20),
                m9 = (a.m8 * bMat.m01) + (a.m9 * bMat.m11) + (a.m10 * bMat.m21),
                m10 = (a.m8 * bMat.m02) + (a.m9 * bMat.m12) + (a.m10 * bMat.m22),
                m11 = (a.m8 * bMat.m03) + (a.m9 * bMat.m13) + (a.m10 * bMat.m23) + a.m11
            };

            return result;
        }

        private static bool IsEnabled()
        {
            return XConfig.fpsVRSocket.Value != 0;
        }
    }
}