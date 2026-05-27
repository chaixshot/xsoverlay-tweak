using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
{
    internal class AlwayUpdateCursor
    {
        private delegate void SyncedUpdateDelegate(Raycaster instance, Unity_Overlay overlay);
        private static readonly SyncedUpdateDelegate SyncedOverlayUpdate = AccessTools.MethodDelegate<SyncedUpdateDelegate>(AccessTools.Method(typeof(Raycaster), "SyncedOverlayUpdate"));

        private static readonly List<Raycaster> RaycasterInstances = [];
        private static readonly Unity_Overlay EmptyOverlay = new();

        [HarmonyPatch(typeof(Raycaster), "Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!IsController(__instance)) return;

            // Add to Update loop array
            RaycasterInstances.Add(__instance);

            // Setting changed
            XConfig.AlwayUpdateCursor.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    RemoveUpdatedOverlay(__instance);
                else
                    AddUpdatedOverlay(__instance);
            };

            EventBridge.InputMethodChanged += () =>
            {
                if (IsEnable())
                    RemoveUpdatedOverlay(__instance);
                else
                    AddUpdatedOverlay(__instance);
            };
        }

        [HarmonyPatch(typeof(Raycaster), "SubscribeToEvents"), HarmonyPatch(typeof(Raycaster), "UnsubscribeFromEvents")]
        [HarmonyPostfix]
        public static void RemoveSubscribeToEvents(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!IsController(__instance)) return;

            RemoveUpdatedOverlay(__instance);
        }

        [HarmonyPatch(typeof(UpdateDateTime), "Update")]
        [HarmonyPostfix]
        public static void DoAlwayUpdateCursor()
        {
            if (!IsEnable()) return;

            // Invoke the delegate stored in the array
            foreach (Raycaster __instance in RaycasterInstances)
                SyncedOverlayUpdate.Invoke(__instance, EmptyOverlay);
        }

        private static void AddUpdatedOverlay(Raycaster __instance)
        {
            // Add listener from overlay update 
            MethodInfo SyncedOverlayUpdate = AccessTools.Method(typeof(Raycaster), "SyncedOverlayUpdate");
            Action<Unity_Overlay> handler = (Action<Unity_Overlay>)Delegate.CreateDelegate(typeof(Action<Unity_Overlay>), __instance, SyncedOverlayUpdate);
            XSOEventSystem.OnUpdatedOverlay += handler;
        }

        private static void RemoveUpdatedOverlay(Raycaster __instance)
        {
            // Remove listener from overlay update 
            MethodInfo SyncedOverlayUpdate = AccessTools.Method(typeof(Raycaster), "SyncedOverlayUpdate");
            Action<Unity_Overlay> handler = (Action<Unity_Overlay>)Delegate.CreateDelegate(typeof(Action<Unity_Overlay>), __instance, SyncedOverlayUpdate);
            XSOEventSystem.OnUpdatedOverlay -= handler;
        }

        private static bool IsEnable()
        {
            return XConfig.AlwayUpdateCursor.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }
        private static bool IsController(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
