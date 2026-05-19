using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading.Tasks;

namespace xsoverlay_tweak;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static Plugin Instance;

    private static readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        XConfig.AllConfig(Config);

        harmony.PatchAll(typeof(Patches.AlwayUpdateCursor));
        harmony.PatchAll(typeof(Patches.AlwaysHideCursor));
        harmony.PatchAll(typeof(Patches.PhysicalMouseDetector));
        harmony.PatchAll(typeof(Patches.MouseNavigation));

        harmony.PatchAll(typeof(Patches.ActivePointerColor));
        harmony.PatchAll(typeof(Patches.PointerDoubleClickDelay));
        harmony.PatchAll(typeof(Patches.PointerScaleMultiply));
        harmony.PatchAll(typeof(Patches.ActivePointerWebView));
        harmony.PatchAll(typeof(Patches.LaserPointer));
        harmony.PatchAll(typeof(Patches.EmulateMouseClickAnimation));

        harmony.PatchAll(typeof(Patches.RefreshRate));

        harmony.PatchAll(typeof(Patches.Setting.SettingPage));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        Instance = this;

        // Set default refresh rate to HMD refresh rate if it's not set by user
        if (XConfig.RefreshRate.Value == -1)
            XConfig.RefreshRate.Value = DeviceManager.Instance.HMDRefreshRate;

        if (XConfig.UpdateNotification.Value)
            Task.Run(Utils.Update.CheckForUpdate);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is started!");
    }
}
