using HarmonyLib;
using System.Diagnostics;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Patches.Keyboard
{
    [HarmonyPatch]
    internal class StutterSpamFix
    {
        private static long lastSentTicks = 0;

        [HarmonyPatch(typeof(Raycaster), "PublishWebViewCursorInput")]
        [HarmonyPostfix]
        public static void PublishWebViewCursorInput(string phase)
        {
            if (!IsEnable()) return;

            switch (phase)
            {
                case "down":
                    lastSentTicks = Stopwatch.GetTimestamp();
                    break;
                case "up":
                    lastSentTicks = Stopwatch.GetTimestamp();
                    break;
                case "cancel":
                    lastSentTicks = Stopwatch.GetTimestamp() + 1000;
                    break;
            }
        }

        [HarmonyPatch(typeof(Raycaster), "ApplyPointerPressUpdate")]
        [HarmonyPostfix]
        public static void ApplyPointerPressUpdate()
        {
            if (!IsEnable()) return;
            lastSentTicks = Stopwatch.GetTimestamp();
        }

        [HarmonyPatch(typeof(Raycaster), "SendCapturedPressClick")]
        [HarmonyPostfix]
        public static void SendCapturedPressClick()
        {
            if (!IsEnable()) return;
            lastSentTicks = Stopwatch.GetTimestamp();
        }

        [HarmonyPatch(typeof(ApiHandler), "OnSendKeyboardEvent")]
        [HarmonyPrefix]
        public static bool OnSendKeyboardEvent()
        {
            if (!IsEnable()) return true;

            if (lastSentTicks == 0)
                return false;

            long now = Stopwatch.GetTimestamp();
            double applyElapsedMs = (now - lastSentTicks) * 1000.0 / Stopwatch.Frequency;
            return applyElapsedMs <= 13f;
        }

        private static bool IsEnable()
        {
            return XConfig.StutterSpamFix.Value;
        }
    }
}
