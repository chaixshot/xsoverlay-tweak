using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR;
using WindowsInput;
using WindowsInput.Native;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    internal class MouseNavigation
    {
        private static ulong ActionHandleBack = 0;
        private static ulong ActionHandleForward = 0;

        private static bool BackWasPressedLastFrame = false;
        private static bool ForwardWasPressedLastFrame = false;

        private static bool IsDesktopHover = false;
        private static Raycaster CurrentRaycaster;

        private static readonly string[] ActionNames = ["/actions/xsoverlay/in/MouseBack", "/actions/xsoverlay/in/MouseForward"];

        // Define this once at the class level to avoid Marshal.SizeOf and repeated allocations
        private static readonly uint DigitalDataSize = (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t));
        private static InputDigitalActionData_t _sharedData = new();

        // Get current active hand
        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void InitializeEvents()
        {
            if (IsEnable())
                ApplySteamVRActionBinding();

            XSOEventSystem.OnTakeControlOfDesktopCursor += raycaster =>
            {
                CurrentRaycaster = raycaster;
            };

            XConfig.MouseNavigation.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    if (ApplySteamVRActionBinding())
                        Utils.Notification.Send($"{MyPluginInfo.PLUGIN_NAME} - Mouse Navigation", $"When enabling Mouse Navigation for the first time, you have to restart {MyPluginInfo.PLUGIN_NAME} to take effect.", 10);
            };
        }

        // Is active hand hover desktop overlay
        [HarmonyPatch(typeof(Raycaster), "Update")]
        [HarmonyPostfix]
        public static void CheckIfHoveringDesktop()
        {
            IsDesktopHover = CurrentRaycaster?.HoveringOverlay && CurrentRaycaster.HoveringOverlay.IsDesktopOrWindowCapture;
        }

        // SteamVR input listen
        [HarmonyPatch(typeof(MouseInputDevice), "Update")]
        [HarmonyPostfix]
        public static void SteamVRKeyBindingListener()
        {
            if (!IsEnable()) return;

            // Back Navigation
            if (CheckActionTriggered("/actions/xsoverlay/in/MouseBack", ref ActionHandleBack, ref BackWasPressedLastFrame))
                if (IsDesktopHover)
                    SimulateBackNavigation(XInputManager.sim);

            // Forward Navigation
            if (CheckActionTriggered("/actions/xsoverlay/in/MouseForward", ref ActionHandleForward, ref ForwardWasPressedLastFrame))
                if (IsDesktopHover)
                    SimulateForwardNavigation(XInputManager.sim);
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

        private static bool ApplySteamVRActionBinding()
        {
            string filePath = @".\XSOverlay_Data\StreamingAssets\SteamVR\actions.json";

            string json = File.ReadAllText(filePath);
            JObject root = JObject.Parse(json);
            bool modified = false;

            // Update "actions" array
            JArray actions = (JArray)root["actions"];

            foreach (string name in ActionNames)
            {
                if (!actions.Any(a => a["name"]?.ToString() == name))
                {
                    actions.Add(new JObject
                    {
                        ["name"] = name,
                        ["type"] = "boolean",
                        ["requirement"] = "optional"
                    });
                    modified = true;
                }
            }

            // Update "localization" object
            // Localization is an array of objects; we want the first one (usually en_US)
            JArray localization = (JArray)root["localization"];
            if (localization?.HasValues == true)
            {
                JObject langObject = (JObject)localization[0];

                if (langObject["/actions/xsoverlay/in/MouseBack"] == null)
                {
                    langObject["/actions/xsoverlay/in/MouseBack"] = "Mouse Back";
                    modified = true;
                }

                if (langObject["/actions/xsoverlay/in/MouseForward"] == null)
                {
                    langObject["/actions/xsoverlay/in/MouseForward"] = "Mouse Forward";
                    modified = true;
                }
            }

            // Save if changes were made
            if (modified)
            {
                File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                Console.WriteLine("Manifest updated with actions and localization.");
            }

            return modified;
        }

        private static bool IsEnable()
        {
            return XConfig.MouseNavigation.Value;
        }
    }
}