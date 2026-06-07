using HarmonyLib;
using System.IO;
using XSOverlay.Websockets.API;

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

        private static bool IsEnable()
        {
            return true;
        }
    }
}
