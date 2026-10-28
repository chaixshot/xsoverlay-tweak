using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valve.VR;

namespace xsoverlay_tweak.Utils
{
    internal class AdvancedHaptics
    {
        private class HapticTracker
        {
            public Coroutine Coroutine;
        }

        private static readonly ConditionalWeakTable<object, HapticTracker> ActiveRumbles = new();
        private static readonly object LeftHandKey = new();
        private static readonly object RightHandKey = new();

        /// <summary>
        /// Sends a haptic rumble with explicit control over frequency and strength.
        /// </summary>
        /// <param name="leftHand">True for left hand, False for right hand.</param>
        /// <param name="duration">Total length of the vibration in seconds.</param>
        /// <param name="frequency">Frequency in Hz (Recommended: 40Hz to 320Hz).</param>
        /// <param name="strength">Strength from 0.0 to 1.0.</param>
        public static void Rumble(bool leftHand, float duration, float frequency, float strength)
        {
            uint deviceIndex = leftHand ? OVR_Pose_Handler.instance.leftIndex : OVR_Pose_Handler.instance.rightIndex;
            object handKey = leftHand ? LeftHandKey : RightHandKey;

            if (deviceIndex == OpenVR.k_unTrackedDeviceIndexInvalid) return;

            // Stop existing coroutine for this hand if active
            if (ActiveRumbles.TryGetValue(handKey, out HapticTracker tracker))
            {
                if (tracker.Coroutine != null)
                    Plugin.Instance.StopCoroutine(tracker.Coroutine);
            }
            else
            {
                tracker = new HapticTracker();
                ActiveRumbles.Add(handKey, tracker);
            }

            tracker.Coroutine = Plugin.Instance.StartCoroutine(DoRumble(tracker, deviceIndex, duration, frequency, strength));
        }

        private static IEnumerator DoRumble(HapticTracker tracker, uint deviceIndex, float duration, float frequency, float strength)
        {
            try
            {
                ushort pulseWidth = (ushort)Mathf.Lerp(0, 6000, Mathf.Clamp01(strength)); // Map strength (0.0 - 1.0) to OpenVR microseconds (0 - 6000)

                if (duration == 0f) // Play once and exit loop immediately for short durations.
                    OpenVR.System?.TriggerHapticPulse(deviceIndex, 0U, pulseWidth);
                else
                {
                    float elapsed = 0f;
                    float targetFrequency = Mathf.Clamp(frequency, 10f, 500f); // Frequency math: Time (seconds) between each pulse = 1 / Frequency
                    float timeBetweenPulses = 1f / targetFrequency;
                    var wait = new WaitForSecondsRealtime(timeBetweenPulses);

                    while (elapsed < duration)
                    {
                        OpenVR.System.TriggerHapticPulse(deviceIndex, 0U, pulseWidth);
                        yield return wait;
                        elapsed += timeBetweenPulses;
                    }
                }
            }
            finally
            {
                if (tracker.Coroutine != null)
                    tracker.Coroutine = null;
            }
        }
    }
}
