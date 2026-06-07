using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Utils.API
{
    internal class CustomAPI
    {
        public static event Action<bool> OnToggleMediaPlayer;


        [HarmonyPatch(typeof(ApiHandler), "InitializeAPI")]
        [HarmonyPostfix]
        public static void InitializeAPI(ApiHandler __instance)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("xsoverlay_tweak.Utils.API.toolbar.js");
            if (stream != null)
            {
                using StreamReader reader = new(stream);
                string jsContent = reader.ReadToEnd();

                string filePath = @".\XSOverlay_Data\StreamingAssets\Plugins\Applications\_UI\Default\_Shared\js\toolbar.js";
                File.WriteAllText(filePath, jsContent);
            }

            __instance.Commands.Add("Tweak_ToggleMediaPlayer", delegate (string sender, string jsonData, string data)
            {
                OnToggleMediaPlayer.Invoke(bool.Parse(jsonData));
            });
        }
    }
}
