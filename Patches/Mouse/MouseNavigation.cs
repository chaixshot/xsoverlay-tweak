using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR;
using WindowsInput;
using WindowsInput.Native;
using XSOverlay;
using xsoverlay_tweak.Utils;
using static XSOverlay.MouseInputDevice;

namespace xsoverlay_tweak.Patches.Mouse
{
    internal class MouseNavigation
    {
        private static ulong ActionHandleBack = 0;
        private static ulong ActionHandleForward = 0;

        private static bool BackWasPressedLastFrame = false;
        private static bool ForwardWasPressedLastFrame = false;

        private class ActionsData
        {
            public string Path;
            public string Label;
        }

        private static class Actions
        {
            public static ActionsData MouseBack = new() { Path = "/actions/xsoverlay/in/MouseBack", Label = "Mouse Back" };
            public static ActionsData MouseForward = new() { Path = "/actions/xsoverlay/in/MouseForward", Label = "Mouse Forward" };
        }

        // Define this once at the class level to avoid Marshal.SizeOf and repeated allocations
        private static readonly uint DigitalDataSize = (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t));
        private static InputDigitalActionData_t _sharedData = new();

        private static float lastTriggerTime;

        // Get current active hand
        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            ApplySteamVRActionBinding();
        }

        // SteamVR input listen
        [HarmonyPatch(typeof(MouseInputDevice), "Update")]
        [HarmonyPostfix]
        public static void SteamVRKeyBindingListener(MouseInputDevice __instance)
        {
            if (!IsEnable()) return;

            if (Time.unscaledTime - lastTriggerTime >= 0.022f) // ~45 FPS
            {
                lastTriggerTime = Time.unscaledTime;

                // Back Navigation
                if (EventBridge.IsHoverAnyDesktopOrWindowCapture)
                    if (CheckActionTriggered(Actions.MouseBack.Path, ref ActionHandleBack, ref BackWasPressedLastFrame))
                    {
                        SimulateBackNavigation(XInputManager.sim);

                        AdvancedHaptics.Rumble(__instance.GrabAxis == ActivationAxis.LeftTrigger, 0.01f, 40f, 0.3f);
                    }

                // Forward Navigation
                if (EventBridge.IsHoverAnyDesktopOrWindowCapture)
                    if (CheckActionTriggered(Actions.MouseForward.Path, ref ActionHandleForward, ref ForwardWasPressedLastFrame))
                    {
                        SimulateForwardNavigation(XInputManager.sim);

                        AdvancedHaptics.Rumble(__instance.GrabAxis == ActivationAxis.LeftTrigger, 0.01f, 40f, 0.3f);
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckActionTriggered(string path, ref ulong handle, ref bool lastState)
        {
            // Fast Handle lookup
            if (handle == 0)
            {
                OpenVR.Input.GetActionHandle(path, ref handle);
                if (handle == 0) return false;
            }

            // Use a shared static struct to avoid re-allocating 'new' on stack
            // We pass it by reference to the OpenVR API
            EVRInputError error = OpenVR.Input.GetDigitalActionData(handle, ref _sharedData, DigitalDataSize, 0);

            // Compacted check: Error and Active state in one branch
            if (error == EVRInputError.None && _sharedData.bActive)
            {
                bool isPressedNow = _sharedData.bState;

                // State-change logic (Trigger on Leading Edge)
                // This is the fastest way to detect a 'Click' (False -> True)
                if (isPressedNow != lastState)
                {
                    lastState = isPressedNow;
                    return isPressedNow;
                }
            }

            return false;
        }

        private static void SimulateBackNavigation(InputSimulator sim)
        {
            if (XConfig.MouseNavigationUseModifiedKey.Value)
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.LEFT);
            else
                sim.Mouse.XButtonClick(1);
        }

        private static void SimulateForwardNavigation(InputSimulator sim)
        {
            if (XConfig.MouseNavigationUseModifiedKey.Value)
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.RIGHT);
            else
                sim.Mouse.XButtonClick(2);
        }

        private static void ApplySteamVRActionBinding()
        {
            string filePath = @".\XSOverlay_Data\StreamingAssets\SteamVR\actions.json";

            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            bool modified = false;
            JObject root = JObject.Parse(json);

            JArray actions = (JArray)root["actions"];
            JArray localization = (JArray)root["localization"];

            // Grab the first language object safely
            JObject langObject = localization?.HasValues == true ? (JObject)localization[0] : null;

            // handle both the action array and localization at the same time
            foreach (FieldInfo field in typeof(Actions).GetFields())
            {
                if (field.GetValue(null) is ActionsData actionData)
                {
                    string actionPath = actionData.Path;
                    string actionName = actionData.Label;

                    if (string.IsNullOrEmpty(actionPath)) continue;

                    // Check and inject into the actions array
                    if (actions != null && !actions.Any(a => a["name"]?.ToString() == actionPath))
                    {
                        actions.Add(new JObject
                        {
                            ["name"] = actionPath,
                            ["type"] = "boolean",
                            ["requirement"] = "optional"
                        });
                        modified = true;
                    }

                    // Check and inject into the localization object
                    if (langObject != null && langObject[actionPath] == null)
                    {
                        langObject[actionPath] = actionName;
                        modified = true;
                    }
                }
            }

            if (modified) // Save if changes were made
            {
                File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                Console.WriteLine("Manifest updated with actions and localization.");
            }
        }

        private static bool IsEnable()
        {
            return XConfig.MouseNavigation.Value;
        }
    }
}