using HarmonyLib;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Mouse;
using xsoverlay_tweak.Patches.Pointer;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class LaserPointer
    {
        private class LaserData
        {
            public Unity_Overlay LaserA;
            public Unity_Overlay LaserB;
            public Texture2D Texture = new(1, 250, TextureFormat.RGBA32, false);
            public float Distance = 1f;
            public float Distance_Last = 1f;
            public Vector3 RayHitPoint_last = new();
            public float LastUpdateLengthTime = 0f;
        }

        private static readonly ConditionalWeakTable<Raycaster, LaserData> LaserDictionary = new();
        private static bool ShouldBeActive = false;

        // Create Laser_A overlay
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;
            if (IsEnable())
                CreateLaser(__instance);

            // Listen for hovering ClickState changes to update Laser_A length immediately when hovering something new
            XSOEventSystem.OnSwitchHoveringOverlay += (hovering, overlay) =>
            {
                if (IsEnable())
                    Plugin.Instance.StartCoroutine(UpdateLaserLengthDelay(hovering));
            };

            // Listen for setting changes to create/destroy lasers when toggling the setting
            XConfig.LaserPointer.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    CreateLaser(__instance);
                else
                {
                    if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
                    {
                        Object.Destroy(Data.LaserA.gameObject);
                        Object.Destroy(Data.LaserB.gameObject);
                        Object.Destroy(Data.Texture); // Prevent GPU memory leak
                    }
                    LaserDictionary.Remove(__instance);
                }
            };
        }

        // Check should render lasers
        [HarmonyPatch("DetermineIfActiveRaycaster")]
        [HarmonyPostfix]
        public static void DetermineIfActiveLaser(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            ShouldBeActive = Overlay_Manager.Instance.editMode || __instance.HoveringOverlay != null;
 
            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
            {
                if (ShouldBeActive)
                    __instance.IsActiveRaycaster = true;

                if (Data.LaserA.gameObject.activeSelf != ShouldBeActive)
                {
                    Data.LaserA.gameObject.SetActive(ShouldBeActive);
                    Data.LaserB.gameObject.SetActive(ShouldBeActive);
                }
            }
        }

        // Change lasers position, rotation and length
        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void HandleLaserMovement(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref GameObject ___VisualCursorElement, ref Unity_Overlay ___VisualCursorElementOverlay, ref Vector3 ___CurrentRayPosition, ref Vector3 ___RayHitPoint, ref Vector3 ___CurrentRayDirection)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;
            if (!ShouldBeActive) return;

            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
            {
                // Handle movement
                {
                    PullTriggerPointerLock.InstanceState.TryGetValue(__instance, out PullTriggerPointerLock.RaycasterState ClickState);

                    Vector3 CurrentRayPosition = ___CurrentRayPosition;
                    Vector3 CurrentRayDirection = ___CurrentRayDirection;
                    Vector3 RayHitPoint = ___RayHitPoint;

                    // Capture overlay UseCursorSmoothing
                    if (!IsEnableMouseSmooth() && __instance?.HoveringOverlay?.UseCursorSmoothing == true)
                    {
                        CurrentRayPosition = __instance.transform.position;
                        CurrentRayDirection = Quaternion.AngleAxis(__instance.RayRotationOffset, __instance.transform.right) * __instance.transform.forward;
                    }
 
                    // Capture overlay backward hit point
                    if (__instance?.HoveringOverlay?.IsDesktopOrWindowCapture == true)
                        RayHitPoint = (CurrentRayPosition + CurrentRayDirection * __instance.FinalSteamVRRaycastResults.fDistance) - (CurrentRayDirection * 0.05f);

                    if (PointerDoubleClickDelay.IsEnable() && (___InputDevice.ClickFreezeActive || ClickState?.IsBlock == true)) // PointerDoubleClickDelay lock RayHitPoint in place
                    {
                        RayHitPoint = Data.RayHitPoint_last;
                        CurrentRayDirection = -(CurrentRayPosition - RayHitPoint).normalized;
                    }
                    else
                        Data.RayHitPoint_last = RayHitPoint;

                    Data.Distance = ___VisualCursorElement.activeSelf ? Vector3.Distance(CurrentRayPosition, RayHitPoint) : 0.5f;

                    Data.LaserA.transform.position = CurrentRayPosition + (CurrentRayDirection * (Data.Distance / 2));
                    Data.LaserA.transform.up = CurrentRayDirection;
                    Data.LaserA.transform.Rotate(0, 180 * (__instance.transform.rotation.y - (__instance.transform.rotation.y - Overlay_Manager.Instance.head.rotation.y)), 0, Space.Self);

                    Data.LaserB.transform.position = Data.LaserA.transform.position;
                    Data.LaserB.transform.up = Data.LaserA.transform.up;
                    Data.LaserB.transform.rotation = Data.LaserA.transform.rotation;
                    Data.LaserB.transform.Rotate(0, 180, 0, Space.Self);

                    if (Mathf.Abs(Data.Distance_Last - Data.Distance) > 0.02f)
                        UpdateLaserLength(__instance);
                }

                // Handle active color
                {
                    Color targetColor = XSettingsManager.Instance.Settings.AccentColor;
                    float targetOpacity = 1f;

                    if (!___VisualCursorElement.activeSelf)
                    {
                        targetColor = Color.gray;
                        targetOpacity = XConfig.InactivePointerOpacity.Value / 100f;
                    }
                    else if (PhysicalMouseDetector.IsPhysicalMovement)
                    {
                        targetColor = Color.gray;
                        targetOpacity = XConfig.InactivePointerOpacity.Value / 100f;
                    }
                    else if (InactivePointerColor.IsEnable() && !EventBridge.IsActiveHand(__instance) && !EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay))
                    {
                        targetColor = Color.red;
                        targetOpacity = XConfig.InactivePointerOpacity.Value / 100f;
                    }

                    Data.LaserA.colorTint = targetColor;
                    Data.LaserA.opacity = targetOpacity;
                    //Data.LaserA.overlay.overlayColor = targetColor;
                    //Data.LaserA.overlay.overlayRenderModelColor = targetColor;

                    Data.LaserB.colorTint = targetColor;
                    Data.LaserB.opacity = targetOpacity;
                    //Data.LaserB.overlay.overlayColor = targetColor;
                    //Data.LaserB.overlay.overlayRenderModelColor = targetColor;
                }
            }
        }

        private static void CreateLaser(Raycaster instance)
        {
            if (LaserDictionary.TryGetValue(instance, out _)) return;

            GameObject VisualCursorElementPrefab = (GameObject)AccessTools.Field(typeof(Raycaster), "VisualCursorElementPrefab").GetValue(instance);
            Unity_Overlay Laser_A;
            Unity_Overlay Laser_B;

            {
                GameObject VisualCursorElement_A = Object.Instantiate(VisualCursorElementPrefab);
                Laser_A = VisualCursorElement_A.GetComponent<Unity_Overlay>();

                VisualCursorElement_A.name = string.Format("Raycaster.{0}.{1}", instance.gameObject.name, "LaserPointerA");

                Laser_A.AutoUpdateOverlayTexture = false;
                Laser_A.overlayName = VisualCursorElement_A.name;
                Laser_A.overlayKey = VisualCursorElement_A.name.ToLower();

                Object.Destroy(Laser_A.GetComponent<UI_RelativeTransformManipulator>());
            }
            {
                GameObject VisualCursorElement_B = Object.Instantiate(VisualCursorElementPrefab);
                Laser_B = VisualCursorElement_B.GetComponent<Unity_Overlay>();

                VisualCursorElement_B.name = string.Format("Raycaster.{0}.{1}", instance.gameObject.name, "LaserPointerB");

                Laser_B.AutoUpdateOverlayTexture = false;
                Laser_B.overlayName = VisualCursorElement_B.name;
                Laser_B.overlayKey = VisualCursorElement_B.name.ToLower();

                Object.Destroy(Laser_B.GetComponent<UI_RelativeTransformManipulator>());
            }

            LaserDictionary.Add(instance, new LaserData { LaserA = Laser_A, LaserB = Laser_B });
            Plugin.Instance.StartCoroutine(UpdateLaserLengthDelay(instance));
        }

        // Wait one frame for UpdateRaycaster to update Distance
        private static IEnumerator UpdateLaserLengthDelay(Raycaster hovering)
        {
            yield return null;
            UpdateLaserLength(hovering);
        }

        private static void UpdateLaserLength(Raycaster hovering)
        {
            if (LaserDictionary.TryGetValue(hovering, out LaserData Data))
            {
                if (Time.unscaledTime - Data.LastUpdateLengthTime < 0.1f) return; // ~10 FPS

                int newHeight = Mathf.Max(1, (int)(Data.Distance * 500));

                if (Data.Texture.height == newHeight) return;

                Data.LastUpdateLengthTime = Time.unscaledTime;

                // Reinitialize keeping the Alpha support active
                Data.Texture.Reinitialize(1, newHeight, TextureFormat.RGBA32, false);

                // Generate the procedural fading color pixels
                Color32[] colors = new Color32[newHeight];

                // Define how many pixels long the fading transition to be
                int fadeLengthInPixels = Mathf.Min(100, newHeight);

                for (int y = 0; y < newHeight; y++)
                {
                    byte alpha = 255; // Default fully opaque

                    // y = 0 is the ending of the beam
                    if (y < fadeLengthInPixels)
                    {
                        float fadeRatio = (float)y / fadeLengthInPixels;
                        alpha = (byte)(255 * fadeRatio);
                    }

                    // Start point face
                    /*{
                        int distanceFromEnd = (newHeight - 1) - y;
                        if (distanceFromEnd < fadeLengthInPixels)
                        {
                            float fadeRatio = (float)distanceFromEnd / fadeLengthInPixels;
                            alpha = (byte)(255 * fadeRatio);
                        }
                    }*/


                    // Use solid white for the base channel data because Unity_Overlay.colorTint 
                    // inside HandleLaserMovement will tint it to your preferred AccentColor automatically!
                    colors[y] = new Color32(255, 255, 255, alpha);
                }

                // Upload the new pixel data array to the GPU
                Data.Texture.SetPixels32(colors);
                Data.Texture.Apply();

                Data.LaserA.overlayTexture = Data.Texture;
                Data.LaserA.overlay.overlayTexture = Data.Texture;
                Data.LaserA.overlay.overlayWidthInMeters = 0.002f;
                Data.LaserA.isDashboardOverlay = false;

                Data.LaserB.overlayTexture = Data.Texture;
                Data.LaserB.overlay.overlayTexture = Data.Texture;
                Data.LaserB.overlay.overlayWidthInMeters = 0.002f;
                Data.LaserB.isDashboardOverlay = false;

                Data.Distance_Last = Data.Distance;
            }
        }

        private static bool IsEnableMouseSmooth()
        {
            return XConfig.LaserPointer.Value == 2;
        }

        private static bool IsEnable()
        {
            return XConfig.LaserPointer.Value != 0;
        }

        private static bool IsRightHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName == Raycaster.HapticDevice.Right;
        }
    }
}
