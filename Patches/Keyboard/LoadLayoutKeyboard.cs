using HarmonyLib;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using XSOverlay;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Keyboard
{
    internal class LoadLayoutKeyboard
    {
        private static bool fristLoad = true;

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

        [HarmonyPatch(typeof(LayoutHandler), "LoadLayout", [])]
        [HarmonyPostfix]
        public static async void LoadKeyboardFromLayout(LayoutHandler __instance, string ___LayoutAssetPath)
        {
            string text = ___LayoutAssetPath + "/Layout_" + __instance.SelectedLayout.ToString() + ".json";
            if (Application.isEditor)
                text = ___LayoutAssetPath + "/Editor_Layout_" + __instance.SelectedLayout.ToString() + ".json";

            if (!IsEnable()) return;
            if (!File.Exists(text)) return;

            JObject root = JObject.Parse(File.ReadAllText(text));
            JToken keyboardData = root["keyboard"];
            Overlay_Manager overlayManager = Overlay_Manager.Instance;
            Unity_Overlay keyboard = overlayManager.Keyboard_Overlay;

            if (fristLoad)
            {
                while (overlayManager.Keyboard.gameObject.activeSelf)
                    await Task.Delay(1);
                await Task.Delay(100);
            }

            if (keyboardData == null) // No keyboard save in Layout
                EventBridge.ToggleKeyboardExecuteAPI(false);
            else
            {
                EventBridge.ToggleKeyboardExecuteAPI(true);

                if (fristLoad)
                    await Task.Delay(200);

                if (keyboardData["position"] is JArray pos && pos.Count == 3)
                    keyboard.transform.localPosition = new Vector3((float)pos[0], (float)pos[1], (float)pos[2]);

                if (keyboardData["rotation"] is JArray rot && rot.Count == 4)
                    keyboard.transform.localRotation = new Quaternion((float)rot[0], (float)rot[1], (float)rot[2], (float)rot[3]);

                if (keyboardData["widthInMeters"] != null)
                {
                    keyboard.widthInMeters = (float)keyboardData["widthInMeters"];
                    keyboard.overlay.overlayWidthInMeters = (float)keyboardData["widthInMeters"];
                }

                if (keyboardData["isPinned"] != null)
                {
                    bool shouldPin = (bool)keyboardData["isPinned"];

                    if (shouldPin != keyboard.isPinned)
                        overlayManager.PinKeyboard();
                }

                if (keyboardData["isLocked"] != null)
                {
                    bool shouldLock = (bool)keyboardData["isLocked"];

                    if (shouldLock != keyboard.IsWindowInteractionLocked)
                        overlayManager.LockKeyboard();
                }
            }

            fristLoad = false;
        }

        public static bool IsEnable() => XConfig.LoadLayoutKeyboard.Value;
    }
}
