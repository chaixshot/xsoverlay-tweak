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
        private static readonly FieldInfo IsMoving = AccessTools.Field(typeof(TrackToObject), "IsMoving");

        [HarmonyPatch(typeof(XSettingsManager), "Awake")]
        [HarmonyPostfix]
        public static void ChangeNotificationTrackerToLeashed(TrackToObject ___NotificationTracker)
        {
            TrackToObject.trackingType defaultTrackingType = ___NotificationTracker.TrackingType;
            trackerInstance = ___NotificationTracker;

            XConfig.NotificationLeashedTracker.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    trackerInstance.TrackingType = TrackToObject.trackingType.Leashed;
                else
                    trackerInstance.TrackingType = defaultTrackingType;
            };

            // Notification shows will trigger the leash position
            CustomAPI.OnShowNotification += (notify) =>
            {
                if (IsEnable())
                {
                    forceUpdate = true;
                    IsMoving.SetValue(trackerInstance, false);
                }
            };

            if (IsEnable())
                trackerInstance.TrackingType = TrackToObject.trackingType.Leashed;
        }

        [HarmonyPatch(typeof(TrackToObject), "LeashedFollow")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> LockSlerpHeight(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patchedSlerp = false;
            bool patchedDistance = false;

            var slerpMethod = AccessTools.Method(typeof(Vector3), nameof(Vector3.Slerp), [typeof(Vector3), typeof(Vector3), typeof(float)]);
            var distanceMethod = AccessTools.Method(typeof(Vector3), nameof(Vector3.Distance), [typeof(Vector3), typeof(Vector3)]);

            var customSlerp = AccessTools.Method(typeof(NotificationLeashedTracker), nameof(CustomLockedSlerp));
            var customDistance = AccessTools.Method(typeof(NotificationLeashedTracker), nameof(CustomLockedDistance));

            for (int i = 0; i < codes.Count; i++)
            {
                // Slerp calculation
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo method && method == slerpMethod)
                {
                    codes[i].operand = customSlerp;
                    patchedSlerp = true;
                }

                // Distance comparison
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo distMethod && distMethod == distanceMethod)
                {
                    codes[i].operand = customDistance;
                    patchedDistance = true;
                }
            }

            if (!patchedSlerp)
                Plugin.Logger.LogError("[LeashedFollowPatch] Failed to find the Vector3.Slerp call instruction!");
            if (!patchedDistance)
                Plugin.Logger.LogError("[LeashedFollowPatch] Failed to find the Vector3.Distance call instruction!");

            return codes;
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
                return 999f;

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

        // Smoothly slides X and Z while forcing Y to 1.0f ONLY when forceUpdate is true
        private static Vector3 CustomLockedSlerp(Vector3 current, Vector3 target, float t)
        {
            // Only lock/flatten height if enabled AND forceUpdate is actively true
            if (!IsEnable() || (XConfig.NotificationLeashedTracker.Value == 1 && !forceUpdate))
                return Vector3.Slerp(current, target, t);

            float targetY = GetTargetY();
            current.y = targetY;
            target.y = targetY;

            Vector3 result = Vector3.Lerp(current, target, t);
            result.y = targetY;
            return result;
        }

        // Ensures the distance calculation ignores Y so the logic knows it arrived on X/Z!
        private static float CustomLockedDistance(Vector3 a, Vector3 b)
        {
            if (!IsEnable() || (XConfig.NotificationLeashedTracker.Value == 1 && !forceUpdate))
                return Vector3.Distance(a, b);

            float targetY = GetTargetY();
            a.y = targetY;
            b.y = targetY;

            float distance = Vector3.Distance(a, b);

            // Once the object arrives within target distance on the X/Z plane, 
            // the movement cycle is finishing, so we can turn off forceUpdate safely.
            if (distance <= 0.0001f)
                forceUpdate = false;

            return distance;
        }

        // Helper to grab the tracking target's height safely dynamically
        private static float GetTargetY()
        {
            if (trackerInstance != null)
            {
                Transform targetTransform = TargetTransform?.GetValue(trackerInstance) as Transform;
                if (targetTransform != null)
                {
                    return targetTransform.position.y + trackerInstance.offset.y;
                }
            }
            return 1.0f; // Fallback default if instance tracking breaks down
        }

        private static bool IsEnable()
        {
            return XConfig.NotificationLeashedTracker.Value != 0;
        }
    }
}