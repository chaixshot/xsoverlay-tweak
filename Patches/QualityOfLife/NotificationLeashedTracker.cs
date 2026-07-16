using HarmonyLib;
using System.Reflection;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    [HarmonyPatch]
    internal class NotificationLeashedTracker
    {
        private static CustomTrackerToObject customTracker;
        private static TrackToObject nativeTracker;
        private static GameObject nativeTrackerGameObject;

        private static readonly FieldInfo NotificationTracker = AccessTools.Field(typeof(XSettingsManager), "NotificationTracker");

        [HarmonyPatch(typeof(XSettingsManager), "Awake")]
        [HarmonyPostfix]
        public static void SwapTrackToObject(XSettingsManager __instance, TrackToObject ___NotificationTracker)
        {
            if (___NotificationTracker == null) return;

            nativeTracker = ___NotificationTracker;
            nativeTrackerGameObject = ___NotificationTracker.gameObject;

            XConfig.NotificationLeashedTracker.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                {
                    if (nativeTracker != null)
                        ExecuteSwap(__instance);
                }
                else if (customTracker != null) // Restore
                {
                    nativeTracker = nativeTrackerGameObject.AddComponent<TrackToObject>();

                    // Restore properties mapped from custom component
                    nativeTracker.TrackingType = TrackToObject.trackingType.Smoothed;
                    nativeTracker.TrackTarget = TrackToObject.trackTar.HMD;
                    nativeTracker.offset = customTracker.offset;
                    nativeTracker.OverrideTrackSpeed = customTracker.OverrideTrackSpeed;
                    nativeTracker.trackSpeed = customTracker.trackSpeed;
                    nativeTracker.lookAtTrackedObject = customTracker.lookAtTrackedObject;
                    nativeTracker.ObjectToLookAt = customTracker.ObjectToLookAt;
                    nativeTracker.overlayToEdit = customTracker.overlayToEdit;
                    nativeTracker.enabled = true;

                    NotificationTracker.SetValue(__instance, nativeTracker); // Re-assign the reference back to XSettingsManager

                    UnityEngine.Object.Destroy(customTracker);
                    customTracker = null;
                }
            };

            if (IsEnable())
                ExecuteSwap(__instance);

            static void ExecuteSwap(XSettingsManager xSettingsManager)
            {
                customTracker = nativeTrackerGameObject.AddComponent<CustomTrackerToObject>();
                customTracker.Setup(nativeTracker);

                UnityEngine.Object.Destroy(nativeTracker);
                nativeTracker = null;

                NotificationTracker.SetValue(xSettingsManager, null); // Tell XSettingsManager that the original component is gone for now
            }
        }

        private static bool IsEnable()
        {
            return XConfig.NotificationLeashedTracker.Value != 0;
        }
    }

    public class CustomTrackerToObject : MonoBehaviour
    {
        // Cached variables from original TrackToObject
        public Unity_SteamVR_Handler svr;
        public Vector3 offset;
        public bool OverrideTrackSpeed;
        public float trackSpeed;
        public bool lookAtTrackedObject;
        public GameObject ObjectToLookAt;
        public Unity_Overlay overlayToEdit;

        // Custom State tracking variables
        private Transform targetTransform;
        private Vector3 lastCheckedTargetPosition;
        private Vector3 lastCheckedForward;
        private Vector3 lastCheckedRight;
        private Vector3 lastCheckedUp;
        private Quaternion lastCheckedTargetRotation;

        private bool isMoving;
        private bool forceUpdate;
        private bool lockHeight;

        private readonly float distToMove = 20 * EventBridge.OneCentimetre;
        private readonly float thresholdUpDown = 20f;
        private readonly float thresholdLeftRight = 10f;

        public void Setup(TrackToObject original)
        {
            offset = original.offset;
            OverrideTrackSpeed = original.OverrideTrackSpeed;
            trackSpeed = original.trackSpeed;
            lookAtTrackedObject = original.lookAtTrackedObject;
            ObjectToLookAt = original.ObjectToLookAt;
            overlayToEdit = original.overlayToEdit;

            svr = (Unity_SteamVR_Handler)UnityEngine.Object.FindObjectOfType(typeof(Unity_SteamVR_Handler));

            CustomAPI.OnShowNotification += (notify) =>
            {
                forceUpdate = true;
                lockHeight = true;
                isMoving = false;
            };
        }

        public void Update()
        {
            targetTransform = (svr != null && svr.hmdObject != null) ? svr.hmdObject.transform : null;

            LeashedFollow(); // Execute Custom Leashed Follow logic

            if (overlayToEdit != null && overlayToEdit.IsAttachedToDevice && overlayToEdit.WorldSpaceSceneImpostor != null)
            {
                overlayToEdit.WorldSpaceSceneImpostor.transform.localPosition = transform.position;
                overlayToEdit.WorldSpaceSceneImpostor.transform.localRotation = transform.rotation;
            }
        }

        private void LeashedFollow()
        {
            if (targetTransform == null) return;

            if (isMoving)
            {
                Vector3 currentPos = transform.position;
                Vector3 targetPos = lastCheckedTargetPosition;
                targetPos += offset.x * lastCheckedRight;
                targetPos += offset.y * lastCheckedUp;
                targetPos += offset.z * lastCheckedForward;

                float distance;
                float speed = OverrideTrackSpeed ? trackSpeed : (float)XSettingsManager.Instance.Settings.PositionDampening;

                if (lockHeight)
                {
                    float targetY = targetTransform.position.y + offset.y;
                    currentPos.y = targetY;
                    targetPos.y = targetY;

                    transform.position = Vector3.Lerp(currentPos, targetPos, speed * Time.deltaTime);
                    transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                }
                else
                    transform.position = Vector3.Slerp(transform.position, targetPos, speed * Time.deltaTime);

                if (lockHeight)
                {
                    float targetY = targetTransform.position.y + offset.y;
                    Vector3 flatA = transform.position; flatA.y = targetY;
                    Vector3 flatB = targetPos; flatB.y = targetY;
                    distance = Vector3.Distance(flatA, flatB);
                }
                else
                    distance = Vector3.Distance(transform.position, targetPos);

                if (lookAtTrackedObject && ObjectToLookAt != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - ObjectToLookAt.transform.position);

                if (distance <= 0.0001f)
                    isMoving = false;

                if (XConfig.NotificationLeashedTracker.Value == 1 && !forceUpdate && distance <= 0.005f)
                    lockHeight = false;
            }

            float distanceDelta = Vector3.Distance(lastCheckedTargetPosition, targetTransform.position);
            bool shouldMove = forceUpdate || distanceDelta > distToMove || CheckRotationThresholds();

            if (shouldMove)
            {
                lastCheckedTargetPosition = targetTransform.position;
                lastCheckedTargetRotation = targetTransform.rotation;
                lastCheckedForward = targetTransform.forward;
                lastCheckedRight = targetTransform.right;
                lastCheckedUp = targetTransform.up;
                isMoving = true;
                forceUpdate = false;
            }
        }

        private bool CheckRotationThresholds()
        {
            if (targetTransform == null) return false;

            Quaternion deltaRotation = Quaternion.Inverse(lastCheckedTargetRotation) * targetTransform.rotation;
            Vector3 eulerDeltas = deltaRotation.eulerAngles;

            float deltaX = Mathf.Abs(Mathf.DeltaAngle(0, eulerDeltas.x));
            float deltaY = Mathf.Abs(Mathf.DeltaAngle(0, eulerDeltas.y));

            float thresholdX = (Mathf.Abs(XSettingsManager.Instance.Settings.NotificationOffsets.y) + 1f) * thresholdUpDown;
            float thresholdY = (Mathf.Abs(XSettingsManager.Instance.Settings.NotificationOffsets.x) + 1f) * thresholdLeftRight;

            return deltaX > thresholdX || deltaY > thresholdY;
        }
    }
}