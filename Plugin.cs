using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading.Tasks;
using xsoverlay_tweak.Utils;

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

        harmony.PatchAll(typeof(GlobalizeJsModule));

        harmony.PatchAll(typeof(CustomAPI));
        harmony.PatchAll(typeof(CustomSettings));
        harmony.PatchAll(typeof(EventBridge));

        // RefreshRate
        harmony.PatchAll(typeof(Patches.RefreshRate));

        // Cursor
        harmony.PatchAll(typeof(Patches.Cursor.AlwaysHideCursor));
        harmony.PatchAll(typeof(Patches.Cursor.AlwaysUpdateCursor));
        harmony.PatchAll(typeof(Patches.Cursor.PhysicalMouseDetector));
        harmony.PatchAll(typeof(Patches.Cursor.MouseSmoothSpeed));
        harmony.PatchAll(typeof(Patches.Cursor.WindowsCursorPointer));

        // Pointer
        harmony.PatchAll(typeof(Patches.Pointer.ActivePointerWebView));
        harmony.PatchAll(typeof(Patches.Pointer.EmulateMouseClickAnimation));
        harmony.PatchAll(typeof(Patches.Pointer.InactivePointerColor));
        harmony.PatchAll(typeof(Patches.Pointer.InactivePointerOpacity));
        harmony.PatchAll(typeof(Patches.Pointer.PointerDoubleClickDelay));
        harmony.PatchAll(typeof(Patches.Pointer.PointerParallelOverlay));
        harmony.PatchAll(typeof(Patches.Pointer.PointerScaleMultiply));
        harmony.PatchAll(typeof(Patches.Pointer.PullTriggerPointerLock));
        harmony.PatchAll(typeof(Patches.Pointer.TwoHandedMode));

        // Wrist
        harmony.PatchAll(typeof(Patches.Wrist.fpsVRSocket));
        harmony.PatchAll(typeof(Patches.Wrist.WristClipDistance));
        harmony.PatchAll(typeof(Patches.Wrist.WristOverPosition));
        harmony.PatchAll(typeof(Patches.Wrist.WristStateRestore));

        // Mouse Navigation
        harmony.PatchAll(typeof(Patches.MouseNavigation));

        // FocusedWindow
        harmony.PatchAll(typeof(Patches.FocusedWindow.ElevatedTaskView));

        // Dashboard
        harmony.PatchAll(typeof(Patches.SteamDashboard));

        // Haptic
        harmony.PatchAll(typeof(Patches.Haptic.GrabHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.KeyboardKeyHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.KeyboardPressHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.OverlaySwapHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.StickyKeyHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.ToggleEditModeHaptic));
        harmony.PatchAll(typeof(Patches.Haptic.WebViewHaptic));

        // Optimization
        harmony.PatchAll(typeof(Patches.Optimization.EfficiencyMode));
        harmony.PatchAll(typeof(Patches.Optimization.uOSCThreadLoop));

        // Quality of Life
        harmony.PatchAll(typeof(Patches.QualityOfLife.DefaultCaptureOverlayTexture));
        harmony.PatchAll(typeof(Patches.QualityOfLife.DoubleClickConfirm));
        harmony.PatchAll(typeof(Patches.QualityOfLife.KeyboardHoldingIndicator));
        harmony.PatchAll(typeof(Patches.QualityOfLife.LaserPointer));
        harmony.PatchAll(typeof(Patches.QualityOfLife.OverlayCurveAutoRefresh));
        harmony.PatchAll(typeof(Patches.QualityOfLife.PinBlockInputNonEditMode));
        harmony.PatchAll(typeof(Patches.QualityOfLife.PullTriggerClickThreshold));
        harmony.PatchAll(typeof(Patches.QualityOfLife.WebViewWiderScroll));
        harmony.PatchAll(typeof(Patches.QualityOfLife.WindowsAccentColor));

        // Fix
        harmony.PatchAll(typeof(Patches.Fix.AdditionalFix));
        harmony.PatchAll(typeof(Patches.Fix.CtrlKeyStickyFix));
        harmony.PatchAll(typeof(Patches.Fix.CursorMovingInteractionFix));
        harmony.PatchAll(typeof(Patches.Fix.HandleScrollingFix));
        harmony.PatchAll(typeof(Patches.Fix.LoadLayoutScaleFix));
        harmony.PatchAll(typeof(Patches.Fix.KeyboardControlButtonStateFix));
        harmony.PatchAll(typeof(Patches.Fix.OverlayGripSlipFix));
        harmony.PatchAll(typeof(Patches.Fix.OverlayRollCurveFix));
        harmony.PatchAll(typeof(Patches.Fix.SteamVR_BetaFix));
        harmony.PatchAll(typeof(Patches.Fix.WebViewFrozenFix));
        harmony.PatchAll(typeof(Patches.Fix.WebViewTexturePixelFormatFix));

        // Community Request
        harmony.PatchAll(typeof(Patches.CommunityRequest.HideInvalidBattery));
        harmony.PatchAll(typeof(Patches.CommunityRequest.LoadLayoutKeyboard));
        harmony.PatchAll(typeof(Patches.CommunityRequest.OverlayAttachSmooth));
        harmony.PatchAll(typeof(Patches.CommunityRequest.OverlayConfirmClose));
        harmony.PatchAll(typeof(Patches.CommunityRequest.MouseButtonSwap));
        harmony.PatchAll(typeof(Patches.CommunityRequest.WindowToolbarGesture));
        harmony.PatchAll(typeof(Patches.CommunityRequest.WindowToolbarKeyboard));

        harmony.PatchAll(typeof(Patches.Setting.SettingPage));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        Instance = this;

        if (XConfig.UpdateNotification.Value)
            Task.Run(() =>
            {
                Utils.Update.CheckForUpdate(false);
            });

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is started!");
    }
}
