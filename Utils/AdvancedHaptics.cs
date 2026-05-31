﻿using System.Collections;
using UnityEngine;
using Valve.VR;

namespace xsoverlay_tweak.Utils
{
    internal class AdvancedHaptics
    {
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

            if (deviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
                Plugin.Instance.StartCoroutine(DoRumble(deviceIndex, duration, frequency, strength));
        }

        private static IEnumerator DoRumble(uint deviceIndex, float duration, float frequency, float strength)
        {
            float elapsed = 0f;

            // Map strength (0.0 - 1.0) to OpenVR microseconds (0 - 3999)
            ushort pulseWidth = (ushort)Mathf.Lerp(0, 3999, Mathf.Clamp01(strength));

            // Frequency math: Time (seconds) between each pulse = 1 / Frequency
            // Clamp frequency to safe hardware limits (typically 10Hz to 500Hz)
            float targetFrequency = Mathf.Clamp(frequency, 10f, 500f);
            float timeBetweenPulses = 1f / targetFrequency;

            var wait = new WaitForSecondsRealtime(timeBetweenPulses);
            while (elapsed < duration)
            {
                // Trigger a single sharp pulse
                OpenVR.System.TriggerHapticPulse(deviceIndex, 0U, pulseWidth);

                // Wait for the calculated interval before pulsing again
                yield return wait;

                elapsed += timeBetweenPulses;
            }
        }
    }
}
