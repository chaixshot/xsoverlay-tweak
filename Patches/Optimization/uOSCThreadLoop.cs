using HarmonyLib;
using System;
using UnityEngine;
using uOSC;

namespace xsoverlay_tweak.Patches.Optimization
{
    internal class uOSCThreadLoop
    {
        private static Action loopFunc_;
        private static uOSC.DotNet.Thread uOSCThread;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenConfigChanged()
        {
            XConfig.uOSCThreadLoop.SettingChanged += (sender, args) =>
            {
                uOSCThread.Stop();
                uOSCThread.Start(loopFunc_);
            };
        }

        [HarmonyPatch(typeof(uOscClient), nameof(uOscClient.Send), [typeof(string), typeof(object[])]), HarmonyPatch(typeof(uOscClient), nameof(uOscClient.Send), [typeof(Message)])]
        [HarmonyPostfix]
        public static void Send()
        {
            if (!IsEnable()) return;

            try
            {
                loopFunc_();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Debug.LogError(ex.StackTrace);
            }
        }

        [HarmonyPatch(typeof(uOSC.DotNet.Thread), "ThreadLoop")]
        [HarmonyPrefix]
        public static bool ThreadLoop(uOSC.DotNet.Thread __instance, Action ___loopFunc_)
        {
            uOSCThread = __instance;
            loopFunc_ = ___loopFunc_;

            return !IsEnable();
        }

        private static bool IsEnable()
        {
            return XConfig.uOSCThreadLoop.Value;
        }
    }
}
