using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using XSOverlay;

namespace xsoverlay_tweak.Patches
{
    internal class AlwayUpdateCursor
    {
        private delegate void SyncedUpdateDelegate(Raycaster instance, Unity_Overlay overlay);
        private struct HandData
        {
            public Raycaster Instance;
            public SyncedUpdateDelegate SyncedOverlayUpdate;
        }
        private static readonly List<HandData> _handArray = [];
        private static readonly Unity_Overlay EmptyOverlay = new();

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!IsController(__instance)) return;

            // Add to Update loop array
            _handArray.Add(new HandData
            {
                Instance = __instance,
                SyncedOverlayUpdate = AccessTools.MethodDelegate<SyncedUpdateDelegate>(AccessTools.Method(typeof(Raycaster), "SyncedOverlayUpdate"))
            });

            // Setting changed
            XConfig.AlwayUpdateCursor.SettingChanged += (sender, args) =>
            {
                if (IsEnable())
                    RemoveUpdatedOverlay(__instance);
                else
                    AddUpdatedOverlay(__instance);
            };
        }

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("SubscribeToEvents"), HarmonyPatch("UnsubscribeFromEvents")]
        [HarmonyPostfix]
        public static void SubscribeToEvents(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!IsController(__instance)) return;

            RemoveUpdatedOverlay(__instance);
        }

        [HarmonyPatch(typeof(Raycaster)), HarmonyPatch("UnsubscribeFromEvents")]
        [HarmonyPostfix]
        public static void UnsubscribeFromEvents(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!IsController(__instance)) return;

            RemoveUpdatedOverlay(__instance);
        }

        [HarmonyPatch(typeof(UpdateDateTime)), HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update()
        {
            if (!IsEnable()) return;

            // Invoke the delegate stored in the array
            foreach (HandData data in _handArray)
                data.SyncedOverlayUpdate?.Invoke(data.Instance, EmptyOverlay);
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
            return XConfig.AlwayUpdateCursor.Value;
        }
        private static bool IsController(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }
    }
}
