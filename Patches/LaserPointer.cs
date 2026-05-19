using HarmonyLib;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class LaserPointer
    {
        private class LaserData
        {
            public Unity_Overlay Laser;
            public Texture2D Texture;
            public float Distance;
            public float Distance_Last;
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

            // Listen for hovering state changes to update laser length immediately when hovering something new
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
                        Object.Destroy(Data.Laser.gameObject);
                    LaserDictionary.Remove(__instance);
                }
            };
        }

        // Check should render lasers
        [HarmonyPatch("DetermineIfActiveRaycaster")]
        [HarmonyPostfix]
        public static void DetermineIfActiveRaycaster(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (Overlay_Manager.Instance.editMode || __instance.HoveringOverlay != null)
            {
                __instance.IsActiveRaycaster = true;

                if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
                    Data.Laser.gameObject.SetActive(true);
            }
            else
                if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
                    Data.Laser.gameObject.SetActive(false);
        }

        // Change lasers position, rotation and length
        [HarmonyPatch("UpdateRaycaster")]
        [HarmonyPostfix]
        public static void UpdateRaycaster(Raycaster __instance, ref GameObject ___VisualCursorElement, ref Vector3 ___CurrentRayPosition, ref Vector3 ___RayHitPoint, ref Vector3 ___CurrentRayDirection)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
            {
                Vector3 RayHitPoint = ___RayHitPoint - (___CurrentRayDirection * 0.015f);

                Data.Distance = ___VisualCursorElement.activeSelf ? Vector3.Distance(___CurrentRayPosition, RayHitPoint) : 0.5f;
                Data.Laser.transform.position = ___CurrentRayPosition + (___CurrentRayDirection * (Data.Distance / 2));
                Data.Laser.transform.up = ___CurrentRayDirection;

                if (Mathf.Abs(Data.Distance_Last - Data.Distance) > 0.01f)
                    UpdateLaserLength(__instance);
            }
        }

        // ActivePointerColor
        [HarmonyPatch("UpdateHoveringOverlay")]
        [HarmonyPostfix]
        public static void UpdateHoveringOverlay(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (XConfig.ActivePointerColor.Value)
                if (LaserDictionary.TryGetValue(__instance, out LaserData Data))
                    Data.Laser.colorTint = ___VisualCursorElementOverlay.colorTint;
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
            LaserDictionary.Add(instance, new LaserData { Laser = laser, Texture = new(1, 1, TextureFormat.RGB24, false), Distance = 1f, Distance_Last = 1f });
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
    }
}
