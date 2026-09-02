using HarmonyLib;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using XSOverlay.PointerInput;
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

        private static readonly AccessTools.FieldRef<Raycaster, GameObject> getVisualCursorElementPrefab = AccessTools.FieldRefAccess<Raycaster, GameObject>("VisualCursorElementPrefab");

        private static readonly AccessTools.FieldRef<AngularPointerSmoothingFilter, bool> isSmoothing = AccessTools.FieldRefAccess<AngularPointerSmoothingFilter, bool>("_hasState");
        private static readonly AccessTools.FieldRef<AngularPointerSmoothingFilter, Vector3> getSmoothPosition = AccessTools.FieldRefAccess<AngularPointerSmoothingFilter, Vector3>("_filteredPosition");
        private static readonly AccessTools.FieldRef<AngularPointerSmoothingFilter, Vector3> getSmoothDirection = AccessTools.FieldRefAccess<AngularPointerSmoothingFilter, Vector3>("_filteredDirection");

        // Create Laser_A overlay
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;
            if (IsEnable())
                CreateLaser(__instance);

            // Listen for hovering ClickState changes to update Laser_A length immediately when hovering something new
            EventBridge.OnSwitchHoveringOverlay += (raycaster, overlay) =>
            {
                if (IsEnable())
                    Plugin.Instance.StartCoroutine(UpdateLaserLengthDelay(raycaster));
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
            if (!LaserDictionary.TryGetValue(__instance, out _)) return;

            ShouldBeActive = Overlay_Manager.Instance.editMode || EventBridge.IsHoverAnyOverlay();

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
        public static void HandleLaserMovement(
            Raycaster __instance,

            MouseInputDevice ___InputDevice,
            GameObject ___VisualCursorElement,

            Vector3 ___RawRayPosition,
            Vector3 ___RawRayDirection,
            Vector3 ___RayHitPoint,

            AngularPointerSmoothingFilter ___PointerSmoothingFilter
            )
        {
            if (!IsEnable()) return;
            if (!ShouldBeActive) return;

            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
            {
                // Handle movement
                {
                    Vector3 position = ___RawRayPosition;
                    Vector3 direction = ___RawRayDirection;
                    Vector3 hitPoint = ___RayHitPoint;

                    // UseCursorSmoothing for laser
                    if (IsEnableMouseSmooth() && isSmoothing(___PointerSmoothingFilter))
                    {
                        position = getSmoothPosition(___PointerSmoothingFilter);
                        direction = getSmoothDirection(___PointerSmoothingFilter);
                    }

                    if (PointerDoubleClickDelay.IsEnable() && (___InputDevice.ClickFreezeActive || PullTriggerPointerLock.ShouldLockPointer(__instance))) // PointerDoubleClickDelay lock RayHitPoint in place
                    {
                        hitPoint = Data.RayHitPoint_last;
                        direction = -(position - hitPoint).normalized;
                    }
                    else
                        Data.RayHitPoint_last = hitPoint;

                    Data.Distance = ___VisualCursorElement.activeSelf ? Vector3.Distance(position, hitPoint) : 0.5f;

                    Data.LaserA.transform.position = position + (direction * (Data.Distance / 2));
                    Data.LaserA.transform.up = direction;
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
                    Data.LaserA.overlay.overlayColor = targetColor;
                    Data.LaserA.overlay.overlayRenderModelColor = targetColor;

                    Data.LaserB.colorTint = targetColor;
                    Data.LaserB.opacity = targetOpacity;
                    Data.LaserB.overlay.overlayColor = targetColor;
                    Data.LaserB.overlay.overlayRenderModelColor = targetColor;
                }
            }
        }

        private static void CreateLaser(Raycaster instance)
        {
            if (LaserDictionary.TryGetValue(instance, out _)) return;

            GameObject VisualCursorElementPrefab = getVisualCursorElementPrefab(instance);
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
        private static IEnumerator UpdateLaserLengthDelay(Raycaster raycaster)
        {
            yield return null;
            UpdateLaserLength(raycaster);
        }

        private static void UpdateLaserLength(Raycaster raycaster)
        {
            if (LaserDictionary.TryGetValue(raycaster, out LaserData Data))
            {
                if (Time.unscaledTime - Data.LastUpdateLengthTime < 0.1f) return; // ~10 FPS

                // Apply endpoint offset (in meters) to adjust laser length before hit point
                bool hoverDesktop = raycaster?.HoveringOverlay?.IsDesktopOrWindowCapture == true;
                float endOffsetInMeters = hoverDesktop ? 0.1f : 0f; // Capture overlay backward hit point
                float adjustedDistance = Mathf.Max(0.01f, Data.Distance - endOffsetInMeters);

                int newHeight = Mathf.Max(1, (int)(adjustedDistance * 500));

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
                Data.LaserA.overlay?.overlayTexture = Data.Texture;
                Data.LaserA.widthInMeters = 0.002f;
                Data.LaserA.isDashboardOverlay = false;

                Data.LaserB.overlayTexture = Data.Texture;
                Data.LaserB.overlay?.overlayTexture = Data.Texture;
                Data.LaserB.widthInMeters = 0.002f;
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
