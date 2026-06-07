using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading.Tasks;
using xsoverlay_tweak.Utils;
using xsoverlay_tweak.Utils.API;

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

        harmony.PatchAll(typeof(CustomAPI));
        harmony.PatchAll(typeof(EventBridge));

        harmony.PatchAll(typeof(Patches.RefreshRate));

        harmony.PatchAll(typeof(Patches.Cursor.AlwaysHideCursor));
        harmony.PatchAll(typeof(Patches.Cursor.AlwaysUpdateCursor));
        harmony.PatchAll(typeof(Patches.Cursor.PhysicalMouseDetector));
        harmony.PatchAll(typeof(Patches.Cursor.MouseSmoothSpeed));
        harmony.PatchAll(typeof(Patches.Cursor.WindowsCursorPointer));

        harmony.PatchAll(typeof(Patches.MouseNavigation));

        harmony.PatchAll(typeof(Patches.Pointer.ActivePointerWebView));
        harmony.PatchAll(typeof(Patches.Pointer.EmulateMouseClickAnimation));
        harmony.PatchAll(typeof(Patches.Pointer.InactivePointerColor));
        harmony.PatchAll(typeof(Patches.Pointer.InactivePointerOpacity));
        harmony.PatchAll(typeof(Patches.Pointer.PointerActiveClick));
        harmony.PatchAll(typeof(Patches.Pointer.PointerDoubleClickDelay));
        harmony.PatchAll(typeof(Patches.Pointer.PointerScaleMultiply));

        harmony.PatchAll(typeof(Patches.SteamDashboard));

        harmony.PatchAll(typeof(Patches.Haptic.GrabHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.KeyboardKeyHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.KeyboardPressHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.OverlaySwapHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.StickyKeyHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.ToggleEditModeHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.WebViewHaptic));

        harmony.PatchAll(typeof(Patches.EfficiencyMode));

        harmony.PatchAll(typeof(Patches.QualityOfLife.DefaultCaptureOverlayTexture));
        harmony.PatchAll(typeof(Patches.QualityOfLife.DoubleClickConfirm));
        harmony.PatchAll(typeof(Patches.QualityOfLife.fpsVRSocket));
        harmony.PatchAll(typeof(Patches.QualityOfLife.LaserPointer));
        harmony.PatchAll(typeof(Patches.QualityOfLife.OverlayCurveAutoRefresh));
        harmony.PatchAll(typeof(Patches.QualityOfLife.PinBlockInputNonEditMode));
        harmony.PatchAll(typeof(Patches.QualityOfLife.PullTriggerClickThreshold));
        harmony.PatchAll(typeof(Patches.QualityOfLife.PullTriggerPointerLock));
        harmony.PatchAll(typeof(Patches.QualityOfLife.WebViewWiderScroll));
        harmony.PatchAll(typeof(Patches.QualityOfLife.WristOverPosition));

        harmony.PatchAll(typeof(Patches.Fix.LoadLayoutScaleFix));
        harmony.PatchAll(typeof(Patches.Fix.OverlayRollCurveFix));
        harmony.PatchAll(typeof(Patches.Fix.CtrlKeyStickyFix));
        harmony.PatchAll(typeof(Patches.Fix.WebViewFix));

        harmony.PatchAll(typeof(Patches.Setting.SettingPage));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        Instance = this;

        if (XConfig.UpdateNotification.Value)
            Task.Run(Utils.Update.CheckForUpdate);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is started!");
    }
}
