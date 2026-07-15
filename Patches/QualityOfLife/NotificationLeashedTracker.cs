using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    [HarmonyPatch]
    internal class NotificationLeashedTracker
    {
        private static TrackToObject trackerInstance;
        private static bool forceUpdate;

        private static readonly FieldInfo TargetTransform = AccessTools.Field(typeof(TrackToObject), "targetTransform");
        private static readonly FieldInfo LastCheckedTargetRotation = AccessTools.Field(typeof(TrackToObject), "LastCheckedTargetRotation");

        [HarmonyPatch(typeof(XSettingsManager), "Awake")]
        [HarmonyPostfix]
        public static void ChangeNotificationTrackerToLeashed(TrackToObject ___NotificationTracker)
        {
            TrackToObject.trackingType defaultTrackingType = ___NotificationTracker.TrackingType;

            trackerInstance = ___NotificationTracker;

            XConfig.NotificationLeashedTracker.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    ___NotificationTracker.TrackingType = TrackToObject.trackingType.Leashed;
                else
                    ___NotificationTracker.TrackingType = defaultTrackingType;
            };

            // Notification shows will trigger the leash position
            CustomAPI.OnShowNotification += (notify) =>
            {
                if (IsEnable())
                    forceUpdate = true;
            };

            if (IsEnable())
                ___NotificationTracker.TrackingType = TrackToObject.trackingType.Leashed;
        }

        [HarmonyPatch(typeof(TrackToObject), "LeashedFollow")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OverrideOriginalLeashAngle(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            MethodInfo angleMethod = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Angle), [typeof(Quaternion), typeof(Quaternion)]);
            MethodInfo overrideAngleMethod = AccessTools.Method(typeof(NotificationLeashedTracker), nameof(GetCustomAngle));

            bool patchedAngle = false;

            for (int i = 0; i < codes.Count; i++)
            {
                // Intercept Quaternion.Angle
                if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == angleMethod)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, overrideAngleMethod));
                    patchedAngle = true;
                    i++;
                }
            }

            if (!patchedAngle)
                Debug.LogError("[NotificationLeashedTracker] Failed to find target instructions to patch!");

            return codes;
        }

        // Overrides the angle variable (originally 'num4')
        public static float GetCustomAngle(float originalAngle)
        {
            if (!IsEnable())
                return originalAngle;

            // If a notification was queued, bypass the calculations and force a movement cycle
            if (forceUpdate)
            {
                forceUpdate = false;
                return 999f;
            }

            if (trackerInstance == null)
                return originalAngle;

            // Retrieve the values of the private fields safely via reflection
            Transform targetTransform = TargetTransform?.GetValue(trackerInstance) as Transform;
            if (targetTransform == null)
                return originalAngle;

            Quaternion lastCheckedTargetRotation = (Quaternion)LastCheckedTargetRotation.GetValue(trackerInstance);

            Quaternion deltaRotation = Quaternion.Inverse(lastCheckedTargetRotation) * targetTransform.rotation;
            Vector3 eulerDeltas = deltaRotation.eulerAngles;

            // Normalize angles to [-180, 180] degrees
            float deltaX = Mathf.Abs(Mathf.DeltaAngle(0, eulerDeltas.x)); // Up/Down
            float deltaY = Mathf.Abs(Mathf.DeltaAngle(0, eulerDeltas.y)); // Left/Right

            float thresholdX = (Mathf.Abs(XSettingsManager.Instance.Settings.NotificationOffsets.y) + 1f) * 20f;
            float thresholdY = (Mathf.Abs(XSettingsManager.Instance.Settings.NotificationOffsets.x) + 1f) * 10f;

            // If either of your custom thresholds are breached, we return 999f
            // to trick the compiler's original (num4 > 35f) check to evaluate to TRUE.
            if (deltaX > thresholdX || deltaY > thresholdY)
                return 999f;

            return 0f; // Otherwise, stay at 0f so it remains under 35f (no movement)
        }

        private static bool IsEnable()
        {
            return XConfig.NotificationLeashedTracker.Value;
        }
    }
}