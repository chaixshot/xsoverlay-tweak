using HarmonyLib;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace xsoverlay_tweak.Patches.Fix
{
    internal class WebViewTexturePixelFormatFix
    {

        [HarmonyPatch]
        public static MethodBase TargetMethod()
        {
            Type targetType = typeof(BaseWebView);
            return targetType.GetMethod("_createTexture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        [HarmonyPostfix]
        public static void Postfix(ref Task<Texture2D> __result, int width, int height)
        {
            VXUtils.WarnIfAbnormallyLarge(width, height);
            Texture2D texture2D = new(width, height, TextureFormat.BGRA32, false, false);
            Texture2D texture2D2 = Texture2D.CreateExternalTexture(width, height, TextureFormat.BGRA32, false, false, texture2D.GetNativeTexturePtr());
            global::UnityEngine.Object.Destroy(texture2D);
            __result = Task.FromResult<Texture2D>(texture2D2);
        }
    }
}
