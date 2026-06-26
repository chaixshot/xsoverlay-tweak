using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.CommunityRequest
{
    internal class LoadLayoutKeyboard
    {
        private static bool isFristKeyboardSpawn = true;

        [HarmonyPatch(typeof(LayoutHandler), "SaveLayout", [])]
        [HarmonyPostfix]
        public static void SaveKeyboardToLayout(LayoutHandler __instance, string ___LayoutAssetPath)
        {
            if (!IsEnable()) return;

            if (Overlay_Manager.Instance.Keyboard.activeSelf)
            {
                string text = ___LayoutAssetPath + "/Layout_" + __instance.SelectedLayout.ToString() + ".json";
                if (Application.isEditor)
                    text = ___LayoutAssetPath + "/Editor_Layout_" + __instance.SelectedLayout.ToString() + ".json";

                if (!File.Exists(text)) return;

                JObject root = JObject.Parse(File.ReadAllText(text));
                Unity_Overlay keyboard = Overlay_Manager.Instance.Keyboard_Overlay;

                root["keyboard"] = new JObject
                {
                    ["position"] = new JArray(keyboard.transform.localPosition.x, keyboard.transform.localPosition.y, keyboard.transform.localPosition.z),
                    ["rotation"] = new JArray(keyboard.transform.localRotation.x, keyboard.transform.localRotation.y, keyboard.transform.localRotation.z, keyboard.transform.localRotation.w),
                    ["widthInMeters"] = keyboard.widthInMeters,
                    ["isPinned"] = keyboard.isPinned,
                    ["isLocked"] = keyboard.IsWindowInteractionLocked,
                };

                File.WriteAllText(text, root.ToString(Formatting.Indented));
            }
        }

        [HarmonyPatch(typeof(Overlay_Manager), nameof(Overlay_Manager.EnableKeyboard))]
        [HarmonyPostfix]
        public static void ListenForFirstKeyboardSpawn()
        {
            if (isFristKeyboardSpawn)
                Task.Run(async () =>
                {
                    await Task.Delay(400);
                    isFristKeyboardSpawn = false;
                });
        }

        [HarmonyPatch(typeof(LayoutHandler), "LoadLayout", [])]
        [HarmonyPostfix]
        public static void LoadKeyboardFromLayout(LayoutHandler __instance, string ___LayoutAssetPath)
        {
            if (!IsEnable()) return;

            string text = ___LayoutAssetPath + "/Layout_" + __instance.SelectedLayout.ToString() + ".json";
            if (Application.isEditor)
                text = ___LayoutAssetPath + "/Editor_Layout_" + __instance.SelectedLayout.ToString() + ".json";

            if (!File.Exists(text)) return;

            JObject root = JObject.Parse(File.ReadAllText(text));
            JToken keyboardData = root["keyboard"];
            Overlay_Manager overlay_Manager = Overlay_Manager.Instance;
            Unity_Overlay keyboard = overlay_Manager.Keyboard_Overlay;
            KeyboardGlobalManager keyboardManager = (KeyboardGlobalManager)AccessTools.Field(typeof(Overlay_Manager), "keyboardManager").GetValue(overlay_Manager);

            if (keyboardData == null) // No keyboard save in Layout
                EventBridge.ExecuteApiToggleKeyboard(false);
            else
            {
                EventBridge.ExecuteApiToggleKeyboard(true);

                Task.Run(async () =>
                {
                    if (isFristKeyboardSpawn)
                        await Task.Delay(300); // Wait for re-center and keyboard summoning

                    if (keyboardData["position"] is JArray pos && pos.Count == 3)
                        keyboard.transform.localPosition = new Vector3((float)pos[0], (float)pos[1], (float)pos[2]);

                    if (keyboardData["rotation"] is JArray rot && rot.Count == 4)
                        keyboard.transform.localRotation = new Quaternion((float)rot[0], (float)rot[1], (float)rot[2], (float)rot[3]);

                    if (keyboardData["widthInMeters"] != null)
                        keyboard.widthInMeters = (float)keyboardData["widthInMeters"];

                    if (keyboardData["isPinned"] != null)
                    {
                        bool shouldPin = (bool)keyboardData["isPinned"];

                        if (shouldPin != keyboard.isPinned)
                        {
                            overlay_Manager.PinKeyboard();
                            overlay_Manager.PinWindowSpecificWindow(keyboard);
                        }
                    }

                    if (keyboardData["isLocked"] != null)
                    {
                        bool shouldLock = (bool)keyboardData["isLocked"];

                        if (shouldLock != keyboard.IsWindowInteractionLocked)
                            overlay_Manager.LockKeyboard();
                    }
                });
            }
        }

        public static bool IsEnable()
        {
            return XConfig.LoadLayoutKeyboard.Value;
        }
    }
}
