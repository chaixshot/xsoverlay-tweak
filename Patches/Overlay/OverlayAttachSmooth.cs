using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Valve.VR;
using Vuplex.WebView;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Overlay
{
    internal class OverlayAttachSmooth
    {
        public class SmoothData
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public bool WasSmooth = false;
            public bool IsChildOverlay = false;

            public bool IsMoving = false;
            public bool IsSmooth = true;
            public bool LockRoll = true;
            public bool LockHover = false;
            public int DistThreshold = 0;
            public int AngleThreshold = 0;
            public int StopThreshold = 5;
        }
        public static readonly ConditionalWeakTable<Unity_Overlay, SmoothData> OverlayStatus = new();

        private static bool IsRecenter = false;
        private static Coroutine RecenterCoroutine;

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.CreateNewOverlayWindow))]
        [HarmonyPostfix]
        public static void CaptureOverlaySpawned(Unity_Overlay ___currOverlay)
        {
            if (!IsEnable()) return;

            if (!OverlayStatus.TryGetValue(___currOverlay, out _))
                OverlayStatus.Add(___currOverlay, new());
        }

        [HarmonyPatch(typeof(Unity_Overlay), "Start")]
        [HarmonyPostfix]
        public static void OverlayOverlaySpawned(Unity_Overlay __instance)
        {
            if (!IsEnable()) return;

            if (__instance.overlayName.Equals("window.settings") || __instance.overlayName.Equals("window.toolbar"))
                if (!OverlayStatus.TryGetValue(__instance, out _))
                    OverlayStatus.Add(__instance, new() { IsChildOverlay = true });
        }

        [HarmonyPatch(typeof(Unity_Overlay), "UpdateOverlay")]
        [HarmonyPostfix]
        public static void UpdateSmoothMovement(Unity_Overlay __instance)
        {
            if (!IsEnable()) return;
            if (__instance.deviceToTrack == Unity_Overlay.OverlayTrackedDevice.None || !__instance.isVisible || __instance.IsHidden || __instance.IsPaused) return;

            if (!OverlayStatus.TryGetValue(__instance, out var Data)) return;

            GameObject impostor = __instance.WorldSpaceSceneImpostor;
            if (impostor == null)
            {
                __instance.SetUpWorldSpaceAttachedDummy();
                impostor = __instance.WorldSpaceSceneImpostor;
                if (impostor == null) return;
            }

            Transform target = impostor.transform;

            Unity_Overlay parentOverlay = null;
            SmoothData parentData = null;

            if (Data.IsChildOverlay)
            {
                parentOverlay = Overlay_Manager.Instance.WindowToolbarMover.ParentOverlay;
                if (parentOverlay != null)
                {
                    OverlayStatus.TryGetValue(parentOverlay, out parentData);
                }
            }

            if (parentData != null)
            {
                Data.IsMoving = parentData.IsMoving;
                Data.IsSmooth = parentData.IsSmooth;
                Data.LockRoll = parentData.LockRoll;

                if (Data.IsSmooth || Data.LockRoll)
                {
                    // Child overlays inherit smoothing from their parent. Since they are separate objects 
                    // in the Unity scene, we derive a "smoothed tracking origin" from the parent window's 
                    // smoothed state to calculate where the child should be in world space.

                    // This ensures the child stays perfectly pinned to the parent even when the parent 
                    // has settled into a smoothed position (IsMoving is false).
                    Quaternion smoothedTrackingRot = parentData.Rotation * Quaternion.Inverse(parentOverlay.transform.rotation);
                    Vector3 smoothedTrackingPos = parentData.Position - (smoothedTrackingRot * parentOverlay.transform.position);
                    Data.Position = smoothedTrackingPos + (smoothedTrackingRot * __instance.transform.position);
                    Data.Rotation = smoothedTrackingRot * __instance.transform.rotation;
                }
            }

            if (Data.IsSmooth || Data.LockRoll)
            {
                if (!Data.WasSmooth || __instance.QueuedToForcePositionUpdate)
                {
                    Data.Position = target.position;
                    Data.Rotation = target.rotation;
                    Data.WasSmooth = true;
                }

                Quaternion targetRotation = target.rotation;
                if (Data.LockRoll)
                    targetRotation = Quaternion.LookRotation(targetRotation * Vector3.forward, Vector3.up);

                if (Data.IsSmooth)
                {
                    if (!Data.IsChildOverlay)
                    {
                        // Logic derived from Raycaster.Grab: Determine if we follow the target or wait for threshold.
                        float dist = Vector3.Distance(Data.Position, target.position);
                        float angle = Quaternion.Angle(Data.Rotation, targetRotation);

                        if (__instance.IsHeld || (parentOverlay != null && parentOverlay.IsHeld))
                            Data.IsMoving = true;
                        else if (Data.LockHover && EventBridge.CurrentHoveringOverlay == __instance)
                            Data.IsMoving = false;
                        else if (dist > (EventBridge.OneCentimetre * Data.DistThreshold) || angle > (EventBridge.OneDegree * Data.AngleThreshold))
                            Data.IsMoving = true;
                        else if (dist < (EventBridge.OneCentimetre * Data.StopThreshold) && angle < (EventBridge.OneDegree * Data.StopThreshold))
                            Data.IsMoving = false;
                    }

                    if (Data.IsMoving || IsRecenter)
                    {
                        // If the window is being held, Grab() is already applying smoothing. 
                        // Rigidly (high dampening) to avoid "lagging" behind the hand.
                        // Otherwise, we use a reduced dampening factor to provide a stable, "lazy" follow effect for HMD movement.
                        bool isHeld = __instance.IsHeld || (parentOverlay != null && parentOverlay.IsHeld);
                        float dampMultiplier = isHeld ? 1f : 0.2f;
                        float posT = Time.deltaTime * XSettingsManager.Instance.Settings.PositionDampening * dampMultiplier;
                        float rotT = Time.deltaTime * XSettingsManager.Instance.Settings.RotationDampening * dampMultiplier;

                        Data.Position = Vector3.Lerp(Data.Position, target.position, posT);
                        Data.Rotation = Quaternion.Slerp(Data.Rotation, targetRotation, rotT);
                    }
                }
                else if (!Data.IsChildOverlay)
                {
                    Data.Position = target.position;
                    Data.Rotation = targetRotation;
                }

                // Use Absolute tracking to allow software-side smoothing; TrackedDeviceRelative is rigid at driver level.
                __instance.overlay.overlayTransformType = VROverlayTransformType.VROverlayTransform_Absolute;

                // Backup local offset (XSOverlay uses transform.position/rotation as relative offset storage for HMD overlays)
                Vector3 originalPos = __instance.transform.position;
                Quaternion originalRot = __instance.transform.rotation;

                __instance.transform.position = Data.Position;
                __instance.transform.rotation = Data.Rotation;

                __instance.SetTransformAbsolute(OVR_Pose_Handler.instance.trackingSpace, __instance.transform);

                // Restore local offset so Raycaster and internal state logic aren't broken by world coordinates
                if (!__instance.IsHeld)
                {
                    __instance.transform.position = originalPos;
                    __instance.transform.rotation = originalRot;
                }
            }
            else if (Data.WasSmooth)
            {
                __instance.overlay.overlayTransformType = VROverlayTransformType.VROverlayTransform_TrackedDeviceRelative;
                __instance.QueuedToForcePositionUpdate = true;
                Data.WasSmooth = false;
                Data.IsMoving = false;
            }
        }

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.RecieveRecenterWindows), []), HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.RecieveRecenterWindows), [typeof(HandSource)])]
        [HarmonyPostfix]
        public static void ListenForRecenter()
        {
            if (!IsEnable()) return;

            IsRecenter = true;

            if (RecenterCoroutine != null)
                Plugin.Instance.StopCoroutine(RecenterCoroutine);

            RecenterCoroutine = Plugin.Instance.StartCoroutine(StopRecenter());
        }

        [HarmonyPatch(typeof(Overlay_Manager), "OnRegisterWebviewOverlay")]
        [HarmonyPostfix]
        public static void EditWindowSettingsUI(OverlayWebView wv)
        {
            if (!IsEnable()) return;

            string jsCode = @"(function() {
                try {
                // Check if Ui components are fully loaded
                    if (!window.Ui || !window.Ui.OverlaySetting || !window.Ui.Toggle || !window.Ui.Divider || !window.Ui.Description) {
                        return 'ERROR: Ui components not fully loaded';
                    }

                    var pageContainer = document.querySelector('.page-container-static');
                    var existingSettingsSection = document.getElementById('hidden_Settings');
                // Find the existing 'Settings' section to insert our new section after it
                    if (!existingSettingsSection) {
                        return 'ERROR: Existing #hidden_Settings section not found';
                    }

                    if (document.getElementById('Smooth')) {
                        return 'INFO: Smooth toggle already injected';
                    }

                // Create a new themed section for the tweak settings
                    var tweakSection = Ui.CreateElement(pageContainer, Ui.HtmlType.div, ['page-section', 'theme-semi-transparent-dark']);
                    tweakSection.id = 'TrackSpaceSmooth_Section';
                    existingSettingsSection.parentNode.insertBefore(tweakSection, existingSettingsSection.nextSibling);
                    tweakSection.style.marginTop = '12px';
                    tweakSection.style.marginBottom = '12px';

                    function UpdateSectionVisibility() {
                        var trackingSpaceEl = document.getElementById('TrackingSpace');
                        if (!tweakSection || !trackingSpaceEl) return;
                        var checked = trackingSpaceEl.querySelector('input:checked');
                        var isWorld = checked && checked.getAttribute('internalName') === 'World';
                        tweakSection.style.display = isWorld ? 'none' : 'block';

                // Ensure the page container has a vertical scrollbar if content overflows
                        if (pageContainer) pageContainer.style.overflowY = isWorld ? 'hidden' : 'auto';
                    }

                    var trackingSpaceEl = document.getElementById('TrackingSpace');
                    if (trackingSpaceEl) trackingSpaceEl.addEventListener('change', UpdateSectionVisibility);

                    var smoothSettingDef = new Ui.OverlaySetting(Ui.ComponentType.Toggle, 'Smooth', '', false);
                    smoothSettingDef.internalName = 'Smooth';

                    var lockRollSettingDef = new Ui.OverlaySetting(Ui.ComponentType.Toggle, 'Lock Roll', '', true);
                    lockRollSettingDef.internalName = 'LockRoll';

                    var lockHoverSettingDef = new Ui.OverlaySetting(Ui.ComponentType.Toggle, 'Lock Hover', '', false);
                    lockHoverSettingDef.internalName = 'LockHover';

                    var distThresholdDef = new Ui.OverlaySetting(Ui.ComponentType.Slider, 'Distance', '', 20, [0, 150, 5], 'Centimetre');
                    distThresholdDef.internalName = 'DistThreshold';

                    var angleThresholdDef = new Ui.OverlaySetting(Ui.ComponentType.Slider, 'Angle', '', 50, [0, 120, 5], 'Degree');
                    angleThresholdDef.internalName = 'AngleThreshold';

                    var stopThresholdDef = new Ui.OverlaySetting(Ui.ComponentType.Slider, 'Stop', '', 5, [0, 100, 1], 'Degree/Centimetre');
                    stopThresholdDef.internalName = 'StopThreshold';

                    if (window.SettingsLayout && window.SettingsLayout.Settings) {
                        window.SettingsLayout.Settings.Smooth = smoothSettingDef;
                        window.SettingsLayout.Settings.LockRoll = lockRollSettingDef;
                        window.SettingsLayout.Settings.LockHover = lockHoverSettingDef;
                        window.SettingsLayout.Settings.DistThreshold = distThresholdDef;
                        window.SettingsLayout.Settings.AngleThreshold = angleThresholdDef;
                        window.SettingsLayout.Settings.StopThreshold = stopThresholdDef;
                    }

                // Manually inject the UI components
                    Ui.Toggle(smoothSettingDef, 'Smooth', false, null, tweakSection);
                    Ui.Description(tweakSection, 'Smooths movement of HMD Overlay.', 'Smooth_Desc');

                    Ui.Divider(tweakSection, 'divider', 'LockRoll_Divider');
                    Ui.Toggle(lockRollSettingDef, 'Lock Roll', true, null, tweakSection);
                    Ui.Description(tweakSection, 'Prevents the Overlay from HMD rolling.', 'LockRoll_Desc');

                    Ui.Divider(tweakSection, 'divider', 'LockHover_Divider');
                    Ui.Toggle(lockHoverSettingDef, 'Lock Hover', false, null, tweakSection);
                    Ui.Description(tweakSection, 'Stops moving while hovering the Overlay.', 'LockHover_Desc');

                    Ui.Divider(tweakSection, 'divider', 'DistThreshold_Divider');
                    Ui.Slider(distThresholdDef, 'Distance', 20, [0, 150, 5], 'Centimetre', tweakSection, 300);
                    if (document.getElementById('DistThreshold')) Ui.UpdateSliderUI(document.getElementById('DistThreshold'), 20);
                    Ui.Description(tweakSection, 'Distance threshold to start moving Overlay.', 'DistThreshold_Desc');

                    Ui.Divider(tweakSection, 'divider', 'AngleThreshold_Divider');
                    Ui.Slider(angleThresholdDef, 'Angle', 50, [0, 120, 5], 'Degree', tweakSection, 300);
                    if (document.getElementById('AngleThreshold')) Ui.UpdateSliderUI(document.getElementById('AngleThreshold'), 50);
                    Ui.Description(tweakSection, 'Angle threshold to start moving Overlay.', 'AngleThreshold_Desc');

                    Ui.Divider(tweakSection, 'divider', 'StopThreshold_Divider');
                    Ui.Slider(stopThresholdDef, 'Stop', 5, [0, 100, 1], 'Degree/Centimetre', tweakSection, 300);
                    if (document.getElementById('StopThreshold')) Ui.UpdateSliderUI(document.getElementById('StopThreshold'), 5);
                    Ui.Description(tweakSection, 'Threshold below which the Overlay stops moving.', 'StopThreshold_Desc');

                    UpdateSectionVisibility();

                    function HandleMessages(msg) {
                        var decoded = Api.Parse(msg);
                        if (decoded.Command === 'UpdateAttachedInformation') {
                            SetMenuSmoothStates(decoded.JsonData);
                        }
                    }

                    function SetMenuSmoothStates(data) {
                        let el;
                        if (el = document.getElementById('Smooth')) el.checked = data.isSmooth;
                        if (el = document.getElementById('LockRoll')) el.checked = data.lockRoll;
                        if (el = document.getElementById('LockHover')) el.checked = data.lockHover;
                    
                        if (el = document.getElementById('DistThreshold')) {
                            el.value = data.distThreshold;
                            Ui.UpdateSliderUI(el, data.distThreshold);
                        }
                        if (el = document.getElementById('AngleThreshold')) {
                            el.value = data.angleThreshold;
                            Ui.UpdateSliderUI(el, data.angleThreshold);
                        }
                        if (el = document.getElementById('StopThreshold')) {
                            el.value = data.stopThreshold;
                            Ui.UpdateSliderUI(el, data.stopThreshold);
                        }

                        UpdateSectionVisibility();
                    }

                    Api.Client.Socket.addEventListener('message', HandleMessages);
                    return 'SUCCESS: Smooth toggle injected';
                } catch (e) {
                    return 'ERROR: ' + e.message;
                }
            })()";

            if (wv.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.WindowSettings)
            {
                wv._webView.WebView.LoadProgressChanged += (sender, args) =>
                {
                    if (args.Type == ProgressChangeType.Finished)
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);

                            wv._webView.WebView.ExecuteJavaScript(jsCode, (result) =>
                            {
                                //Plugin.Logger.LogError($"[{wv.UserInterfaceSelection}] {result}");
                            });
                        });
                    }
                };
            }
        }

        [HarmonyPatch(typeof(ApiHandler), "OnSetActiveOverlaySetting")]
        [HarmonyPostfix]
        public static void WindowSetting_SetSetting(string sender, string jsonData)
        {
            if (!IsEnable()) return;

            Unity_Overlay targetOverlay = Overlay_Manager.Instance.WindowSettingsMenuParentOverlay;
            if (!targetOverlay) return;

            if (OverlayStatus.TryGetValue(targetOverlay, out var Data))
            {
                Objects.RequestSetSettingObject requestSetSettingObject = JsonConvert.DeserializeObject<Objects.RequestSetSettingObject>(jsonData);
                string internalName = requestSetSettingObject.internalName;
                string value = requestSetSettingObject.value;

                switch (internalName)
                {
                    case "Smooth":
                        Data.IsSmooth = bool.Parse(value);
                        break;
                    case "LockRoll":
                        Data.LockRoll = bool.Parse(value);
                        break;
                    case "LockHover":
                        Data.LockHover = bool.Parse(value);
                        break;
                    case "DistThreshold":
                        Data.DistThreshold = int.Parse(value);
                        break;
                    case "AngleThreshold":
                        Data.AngleThreshold = int.Parse(value);
                        break;
                    case "StopThreshold":
                        Data.StopThreshold = int.Parse(value);
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(ApiHandler), "OnRequestActiveOverlayInformation", [typeof(string), typeof(string), typeof(Unity_Overlay)])]
        [HarmonyPostfix]
        public static void WindowSetting_GetSetting(ApiHandler __instance, string sender, string data, Unity_Overlay overlay = null)
        {
            if (!IsEnable()) return;

            Unity_Overlay targetOverlay = (overlay ?? Overlay_Manager.Instance.WindowSettingsMenuParentOverlay);
            if (!targetOverlay) return;
            if (!targetOverlay.WindowCaptureAPI) return;
            if (targetOverlay.WindowCaptureAPI.window == null) return;

            if (OverlayStatus.TryGetValue(targetOverlay, out var Data))
            {
                string text = JsonConvert.SerializeObject(new AttachedInformation
                {
                    isSmooth = Data.IsSmooth,
                    lockRoll = Data.LockRoll,
                    lockHover = Data.LockHover,
                    distThreshold = Data.DistThreshold,
                    angleThreshold = Data.AngleThreshold,
                    stopThreshold = Data.StopThreshold,
                });

                __instance.SendMessage("UpdateAttachedInformation", text, null, Enums.MessageTarget.All);
            }
        }

        [HarmonyPatch(typeof(LayoutHandler), "SaveLayout", [])]
        [HarmonyPostfix]
        public static void SaveOverlayStatusToLayout(LayoutHandler __instance, string ___LayoutAssetPath)
        {
            if (!IsEnable()) return;

            string text = ___LayoutAssetPath + "/Layout_" + __instance.SelectedLayout.ToString() + ".json";
            if (Application.isEditor)
                text = ___LayoutAssetPath + "/Editor_Layout_" + __instance.SelectedLayout.ToString() + ".json";

            if (!File.Exists(text)) return;

            List<Unity_Overlay> activeOverlayComponents = Overlay_Manager.Instance.ActiveOverlayComponents;
            JObject root = JObject.Parse(File.ReadAllText(text));

            bool modified = false;
            for (int i = 0; i < activeOverlayComponents.Count; i++)
            {
                Unity_Overlay unity_Overlay = activeOverlayComponents[i];

                if (OverlayStatus.TryGetValue(unity_Overlay, out var Data))
                {
                    root["windows"][i]["HmdSmooth"] = new JObject
                    {
                        ["isSmooth"] = Data.IsSmooth,
                        ["lockRoll"] = Data.LockRoll,
                        ["lockHover"] = Data.LockHover,
                        ["distThreshold"] = Data.DistThreshold,
                        ["angleThreshold"] = Data.AngleThreshold,
                        ["stopThreshold"] = Data.StopThreshold,
                    };
                    modified = true;
                }
            }

            if (modified)
                File.WriteAllText(text, root.ToString(Formatting.Indented));
        }

        [HarmonyPatch(typeof(LayoutHandler), "LoadLayout", [])]
        [HarmonyPostfix]
        public static void LoadOverlayStatusFromLayout(LayoutHandler __instance, string ___LayoutAssetPath)
        {
            if (!IsEnable()) return;

            string text = ___LayoutAssetPath + "/Layout_" + __instance.SelectedLayout.ToString() + ".json";
            if (Application.isEditor)
                text = ___LayoutAssetPath + "/Editor_Layout_" + __instance.SelectedLayout.ToString() + ".json";

            if (!File.Exists(text)) return;

            List<Unity_Overlay> activeOverlayComponents = Overlay_Manager.Instance.ActiveOverlayComponents;
            JObject root = JObject.Parse(File.ReadAllText(text));

            for (int i = 0; i < activeOverlayComponents.Count; i++)
            {
                Unity_Overlay unity_Overlay = activeOverlayComponents[i];

                if (OverlayStatus.TryGetValue(unity_Overlay, out var Data))
                {
                    JToken HmdSmooth = root["windows"][i]["HmdSmooth"];

                    if (HmdSmooth != null)
                    {
                        Data.IsSmooth = (bool?)(HmdSmooth["isSmooth"]) ?? false;
                        Data.LockRoll = (bool?)(HmdSmooth["lockRoll"]) ?? false;
                        Data.LockHover = (bool?)(HmdSmooth["lockHover"]) ?? false;
                        Data.DistThreshold = (int?)(HmdSmooth["distThreshold"]) ?? 0;
                        Data.AngleThreshold = (int?)(HmdSmooth["angleThreshold"]) ?? 0;
                        Data.StopThreshold = (int?)(HmdSmooth["stopThreshold"]) ?? 0;
                    }
                }
            }
        }

        private static IEnumerator StopRecenter()
        {
            yield return new WaitForSecondsRealtime(1f);

            IsRecenter = false;
            RecenterCoroutine = null;
        }

        private static bool IsEnable()
        {
            return XConfig.OverlayAttachSmooth.Value;
        }

        private struct AttachedInformation
        {
            public bool isSmooth;
            public bool lockRoll;
            public bool lockHover;
            public int distThreshold;
            public int angleThreshold;
            public int stopThreshold;
        }
    }
}