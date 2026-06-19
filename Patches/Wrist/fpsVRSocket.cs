using HarmonyLib;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Valve.VR;
using XSOverlay;
using xsoverlay_tweak.Patches.CommunityRequest;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Wrist
{
    internal class fpsVRSocket
    {
        private static CVROverlay Overlay;
        private static EVROverlayError evroverlayError;
        private static ulong fpsVRHandle = 0;

        private static bool IsClosing = false;
        private static bool IsPerformanceMonitor = false;
        private static bool IsMediaPlayer = false;
        private static Coroutine ClosingCoroutine;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void Awake()
        {
            XSOEventSystem.OnToggleLayoutMode += (enable) =>
            {
                if (!IsEnabled()) return;

                if (XConfig.fpsVRSocket.Value == 1 || XConfig.fpsVRSocket.Value == 2) // Top, Bottom
                    RefreshWristState(enable, 0.4f);
            };

            XSOEventSystem.OnStartStopPerformanceMonitor += (enable) =>
            {
                if (!IsEnabled()) return;

                IsPerformanceMonitor = enable;

                if (XConfig.fpsVRSocket.Value == 1 || XConfig.fpsVRSocket.Value == 2) // Top, Bottom
                    RefreshWristState(enable, 0.4f);
            };

            CustomAPI.OnToggleMediaPlayer += (enable) =>
            {
                if (!IsEnabled()) return;

                IsMediaPlayer = enable;

                if (XConfig.fpsVRSocket.Value == 2) // Bottom
                    RefreshWristState(enable, 0.4f);
            };

            CustomAPI.OnClickToggleMediaPlayer += (enable) =>
            {
                if (!IsEnabled()) return;

                IsMediaPlayer = enable;

                if (XConfig.fpsVRSocket.Value == 2) // Bottom
                    RefreshWristState(enable, 0.4f);
            };

            XConfig.fpsVRSocket.SettingChanged += (sender, args) =>
            {
                if (!IsEnabled()) return;

                ChangefpsVRTranform();
            };

            XConfig.HideInvalidBattery.SettingChanged += async (sender, args) =>
            {
                if (!IsEnabled()) return;

                await Task.Delay(100); // Forget why is here
                ChangefpsVRTranform();
            };

            XConfig.HideBattery.SettingChanged += (sender, args) =>
            {
                if (!IsEnabled()) return;

                ChangefpsVRTranform();
            };
        }

        [HarmonyPatch(typeof(Unity_Overlay), "FadeOverlayIn")]
        [HarmonyPostfix]
        public static void WristOverlayFadedIn(Unity_Overlay __instance)
        {
            if (!IsEnabled()) return;

            if (__instance.IsWristOverlay)
                if (!IsClosing)
                    if (__instance.overlay.overlayTexture == null) // First visible
                        RefreshWristState(false, 0.1f); // Wait for IsHidden == false to fire UpdateTexture()
                    else
                        ChangefpsVRTranform();
        }

        [HarmonyPatch(typeof(WindowMovementManager), "DoScaleWindowFixed")]
        [HarmonyPostfix]
        public static void UpdateOnScaling(Unity_Overlay activeOverlay)
        {
            if (!IsEnabled()) return;

            if (WristOverlay.Instance.overlay == activeOverlay)
                ChangefpsVRTranform();
        }

        [HarmonyPatch(typeof(WindowMovementManager), "DetermineIfOverlayShouldBeCurved")]
        [HarmonyPostfix]
        public static void UpdateOnMoving(Unity_Overlay overlay)
        {
            if (!IsEnabled()) return;

            if (WristOverlay.Instance.overlay == overlay)
                ChangefpsVRTranform();
        }

        private static void ChangefpsVRTranform()
        {
            if (fpsVRHandle == 0)
            {
                Overlay = OpenVR.Overlay;
                evroverlayError = Overlay.FindOverlay("sb.fpsVR.ingameoverlay", ref fpsVRHandle);

                if (evroverlayError != EVROverlayError.None)
                    Plugin.Logger.LogError(evroverlayError.ToString());
            }

            if (fpsVRHandle == 0 || WristOverlay.Instance == null || WristOverlay.Instance.overlay.overlay.overlayTexture == null) return;

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
                bool isBattery = !HideInvalidBattery.IsEnable() && !HideInvalidBattery.Devices.Count.Equals(0);

                yOffset += (xsoHeightInMeters * +0.3f);

                if (IsPerformanceMonitor && Overlay_Manager.Instance.editMode && !isBattery)
                    yOffset += (xsoHeightInMeters * -0.23f);
                else if (IsMediaPlayer)
                    yOffset += (xsoHeightInMeters * -0.16f);
                else if (IsPerformanceMonitor && !isBattery)
                    yOffset += (xsoHeightInMeters * -0.08f);

                if (!isBattery)
                    yOffset += (xsoHeightInMeters * +0.15f);

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
                //evroverlayError = Overlay.SetOverlayTransformAbsolute(fpsVRHandle, overlayTransformAbsoluteTrackingOrigin, ref overlayTransform);
            }
            else // ExecuteJava tracking changes to the target handle destination
            {
                uint overlayTransformTrackedDeviceRelativeIndex = WristOverlay.Instance.overlay.overlay.overlayTransformTrackedDeviceRelativeIndex;
                evroverlayError = Overlay.SetOverlayTransformTrackedDeviceRelative(fpsVRHandle, overlayTransformTrackedDeviceRelativeIndex, ref overlayTransform);
            }

            if (evroverlayError != EVROverlayError.None)
                Plugin.Logger.LogError(evroverlayError.ToString());
        }

        private static IEnumerator ClosingDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

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

        private static void RefreshWristState(bool enable, float delay = 0f)
        {
            if (ClosingCoroutine != null)
            {
                Plugin.Instance.StopCoroutine(ClosingCoroutine);
                ClosingCoroutine = null;
            }

            IsClosing = !enable;

            if (IsClosing)
                ClosingCoroutine = Plugin.Instance.StartCoroutine(ClosingDelay(delay));
            else
                ChangefpsVRTranform();
        }

        private static bool IsEnabled()
        {
            return XConfig.fpsVRSocket.Value != 0;
        }
    }
}