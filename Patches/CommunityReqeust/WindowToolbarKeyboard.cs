using HarmonyLib;
using System.IO;
using XSOverlay;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.CommunityReqeust
{
    internal class WindowToolbarKeyboard
    {
        [HarmonyPatch(typeof(ApiHandler), "InitializeAPI")]
        [HarmonyPostfix]
        public static void AddWindowToolbarKeybordButton(ApiHandler __instance)
        {
            string filePath = @".\XSOverlay_Data\StreamingAssets\Plugins\Applications\_UI\Default\_Shared\js\toolbar.js";
            if (!File.Exists(filePath)) return;

            string content = File.ReadAllText(filePath);
            string original = "var windowToolbarLookup = {\r\n    \"WindowSettings\": \"gear-fill\",";
            string edited = "var windowToolbarLookup = {\r\n    \"Keyboard\": \"keyboard-fill\",\r\n\t\"WindowSettings\": \"gear-fill\",";

            if (content.Contains(edited))
            {
                if (!IsEnable())
                {
                    string patched = content.Replace(edited, original);
                    File.WriteAllText(filePath, patched);
                }
            }
            else
            {
                if (content.Contains(original))
                {
                    string patched = content.Replace(original, edited);
                    File.WriteAllText(filePath, patched);
                }
            }
        }

        [HarmonyPatch(typeof(OverlayWebView), "Awake")]
        [HarmonyPrefix]
        public static bool ChangeWindowToolbarDimension(OverlayWebView __instance)
        {
            if (!IsEnable()) return true;

            if (__instance.UserInterfaceSelection == OverlayWebView.UserInterfacePaths.WindowToolbar)
                __instance.Width += 110;

            return true;
        }

        [HarmonyPatch(typeof(Overlay_Manager), "EnableKeyboard")]
        [HarmonyPostfix]
        public static void SpawnKeyboardPostionFix(Overlay_Manager __instance, KeyboardGlobalManager ___keyboardManager)
        {
            if (!IsEnable()) return;

            if (EventBridge.CurrentHoveringOverlay?.overlayName == "window.toolbar" && ___keyboardManager?.HasKeyboardBeenOpened == true)
            {
                __instance.Keyboard_Overlay.transform.position = OverlaySwitcher.Instance.ToolBarWindowOverlay.transform.position;
                __instance.Keyboard_Overlay.transform.rotation = OverlaySwitcher.Instance.ToolBarWindowOverlay.transform.rotation;
                __instance.Keyboard_Overlay.transform.position += __instance.Keyboard_Overlay.transform.forward * -0.2f;
            }
        }

        private static bool IsEnable()
        {
            return true;
        }
    }
}
