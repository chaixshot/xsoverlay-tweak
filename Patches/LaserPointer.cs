using HarmonyLib;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class LaserPointer
    {
        private class LaserData
        {
            public Unity_Overlay Laser;
            public Texture2D Texture = new(1, 250, TextureFormat.RGB24, false);
            public float Distance = 1f;
            public float Distance_Last = 1f;
            public Vector3 RayHitPoint_last = new();
        }

        private static readonly ConditionalWeakTable<Raycaster, LaserData> LaserDictionary = new();

        // Create laser overlay
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!IsHand(__instance)) return;
            if (IsEnable())
                CreateLaser(__instance);

            // Listen for hovering DoubleClickDelayState changes to update laser length immediately when hovering something new
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
                        Object.Destroy(Data.Laser.gameObject);
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
            if (!IsHand(__instance)) return;

            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
                if (Overlay_Manager.Instance.editMode || __instance.HoveringOverlay != null)
                {
                    __instance.IsActiveRaycaster = true;

                    Data.Laser.gameObject.SetActive(true);
                }
                else
                    Data.Laser.gameObject.SetActive(false);
        }

        // Change lasers position, rotation and length
        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void HandleLaserMovement(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref GameObject ___VisualCursorElement, ref Unity_Overlay ___VisualCursorElementOverlay, ref Vector3 ___CurrentRayPosition, ref Vector3 ___RayHitPoint, ref Vector3 ___CurrentRayDirection)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
            {
                // Handle movement
                {
                    PullTriggerPointerLock.InstanceState.TryGetValue(__instance, out PullTriggerPointerLock.RaycasterState DoubleClickDelayState);

                    Vector3 CurrentRayPosition = ___CurrentRayPosition;
                    Vector3 CurrentRayDirection = ___CurrentRayDirection;
                    Vector3 RayHitPoint = ___RayHitPoint;

                    // Capture overlay UseCursorSmoothing
                    if (XConfig.LaserPointerMouseSmoothDisable.Value && __instance?.HoveringOverlay?.UseCursorSmoothing == true)
                    {
                        CurrentRayPosition = __instance.transform.position;
                        CurrentRayDirection = Quaternion.AngleAxis(__instance.RayRotationOffset, __instance.transform.right) * __instance.transform.forward;
                        RayHitPoint = (CurrentRayPosition + CurrentRayDirection * __instance.FinalSteamVRRaycastResults.fDistance) - (CurrentRayDirection * 0.05f);
                    }

                    if (PointerDoubleClickDelay.IsEnable() && (___InputDevice.ClickFreezeActive || (DoubleClickDelayState != null && DoubleClickDelayState.IsBlock))) // PointerDoubleClickDelay lock RayHitPoint in place
                    {
                        RayHitPoint = Data.RayHitPoint_last;
                        CurrentRayDirection = -(CurrentRayPosition - RayHitPoint).normalized;
                    }
                    else
                        Data.RayHitPoint_last = RayHitPoint;

                    Data.Distance = ___VisualCursorElement.activeSelf ? Vector3.Distance(CurrentRayPosition, RayHitPoint) : 0.5f;
                    Data.Laser.transform.position = CurrentRayPosition + (CurrentRayDirection * (Data.Distance / 2));
                    Data.Laser.transform.up = CurrentRayDirection;
                    Data.Laser.transform.Rotate(0, 180 * (__instance.transform.rotation.y - (__instance.transform.rotation.y - Overlay_Manager.Instance.head.rotation.y)), 0, Space.Self);

                    if (Mathf.Abs(Data.Distance_Last - Data.Distance) > 0.01f)
                        UpdateLaserLength(__instance);
                }

                // Handle active color
                {
                    if (___VisualCursorElement.activeSelf) // Hover any Overlay
                    {
                        if (!ActivePointerColor.IsEnable() || EventBridge.IsActiveHand(__instance) || EventBridge.IsOverlayKeyboard(__instance.HoveringOverlay))
                        {
                            Data.Laser.colorTint = XSettingsManager.Instance.Settings.AccentColor;
                            Data.Laser.opacity = 1f;
                        }
                        else
                        {
                            Data.Laser.colorTint = Color.red;
                            Data.Laser.opacity = XConfig.ActivePointerOpacity.Value / 100f;
                        }
                    }
                    else
                    {
                        Data.Laser.colorTint = Color.gray;
                        Data.Laser.opacity = XConfig.ActivePointerOpacity.Value / 100f;
                    }

                    Data.Laser.overlay.overlayColor = Data.Laser.colorTint;
                    Data.Laser.overlay.overlayRenderModelColor = Data.Laser.colorTint;
                }
            }
        }

        private static void CreateLaser(Raycaster instance)
        {
            GameObject VisualCursorElementPrefab = (GameObject)AccessTools.Field(typeof(Raycaster), "VisualCursorElementPrefab").GetValue(instance);
            GameObject VisualCursorElement = Object.Instantiate(VisualCursorElementPrefab);
            Unity_Overlay laser = VisualCursorElement.GetComponent<Unity_Overlay>();

            VisualCursorElement.name = string.Format("Raycaster.{0}.{1}", instance.gameObject.name, "LaserPointer");

            laser.AutoUpdateOverlayTexture = false;
            laser.overlayName = VisualCursorElement.name;
            laser.overlayKey = VisualCursorElement.name.ToLower();

            Object.Destroy(laser.GetComponent<UI_RelativeTransformManipulator>());
            LaserDictionary.Add(instance, new LaserData { Laser = laser });
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
                int newHeight = Mathf.Max(1, (int)(Data.Distance * 500));

                Data.Texture.Reinitialize(1, newHeight);
                Data.Texture.Apply(); // Apply changes to the GPU.

                Data.Laser.overlayTexture = Data.Texture;
                Data.Laser.overlay.overlayTexture = Data.Texture;
                Data.Laser.overlay.overlayWidthInMeters = 0.002f;
                Data.Laser.isDashboardOverlay = false;

                Data.Distance_Last = Data.Distance;
            }
        }

        private static bool IsEnable()
        {
            return XConfig.LaserPointer.Value;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }

        private static bool IsRightHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName == Raycaster.HapticDevice.Right;
        }
    }
}
