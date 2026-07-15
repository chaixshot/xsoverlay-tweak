using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Valve.Newtonsoft.Json;
using XSOverlay.WebApp;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Patches.Wrist
{
    internal class HideInvalidBattery
    {
        public static Dictionary<uint, DeviceManager.Device> Devices;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenForConfigChange()
        {
            XConfig.HideInvalidBattery.SettingChanged += (sender, args) =>
            {
                ServerClientBridge.Instance.Api.Commands["RequestDeviceInformation"]("systemui_GlobalToolbar", "", "");
            };
        }

        [HarmonyPatch(typeof(ApiHandler), "OnRequestDeviceInformation")]
        [HarmonyPrefix]
        public static bool UpdateDevices(ApiHandler __instance, string sender, string data)
        {
            Devices = new(DeviceManager.Instance.Devices);

            if (!IsEnable()) return true;

            if (IsHideEnable())
                Devices = [];
            else
            {
                foreach (KeyValuePair<uint, DeviceManager.Device> item in Devices.ToList())
                    if (item.Value.battery == 0 || !item.Value.isSupported)
                        Devices.Remove(item.Key);
            }

            IOrderedEnumerable<DeviceManager.Device> orderedEnumerable = from x in Devices.Values.ToList<DeviceManager.Device>()
                                                                         orderby x.classification, x.id == OVR_Pose_Handler.instance.leftIndex descending, x.id
                                                                         select x;

            __instance.SendMessage("UpdateDeviceInformation", JsonConvert.SerializeObject(orderedEnumerable), null, sender);

            return false;
        }

        public static bool IsEnable()
        {
            return XConfig.HideInvalidBattery.Value || IsHideEnable();
        }

        public static bool IsHideEnable()
        {
            return XConfig.HideBattery.Value;
        }
    }
}
