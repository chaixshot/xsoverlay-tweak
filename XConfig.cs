using BepInEx.Configuration;

namespace xsoverlay_tweak
{
    internal class XConfig
    {
        public static ConfigEntry<int> RefreshRate;
        public static ConfigEntry<bool> OnlyHoverOverlay;
        public static ConfigEntry<bool> OnlyInLayoutMod;

        public static ConfigEntry<bool> AlwayUpdateCursor;
        public static ConfigEntry<bool> AlwaysHideCursor;
        public static ConfigEntry<bool> PhysicalMouseDetector;
        public static ConfigEntry<float> MouseSmoothSpeed;

        public static ConfigEntry<bool> ActivePointerColor;
        public static ConfigEntry<int> ActivePointerOpacity;
        public static ConfigEntry<bool> ActivePointerWebView;
        public static ConfigEntry<int> PointerScaleMultiply;
        public static ConfigEntry<bool> PointerDoubleClickDelay;
        public static ConfigEntry<bool> PointerActiveClick;
        public static ConfigEntry<bool> EmulateMouseClickAnimation;
        public static ConfigEntry<bool> LaserPointer;

        public static ConfigEntry<bool> MouseNavigation;
        public static ConfigEntry<bool> MouseNavigationUseModifiedKey;

        public static ConfigEntry<bool> DashboardNotification;
        public static ConfigEntry<bool> DashboardPointer;
        public static ConfigEntry<bool> DashboardSettings;
        public static ConfigEntry<bool> DashboardWindow;
        public static ConfigEntry<bool> DashboardWrist;
        public static ConfigEntry<bool> Dashboardkeyboard;

        public static ConfigEntry<bool> EfficiencyMode;
        public static ConfigEntry<int> InactiveRefreshRate;
        public static ConfigEntry<bool> WristOverPosition;
        public static ConfigEntry<bool> WebViewFix;
        public static ConfigEntry<bool> WebViewWiderScroll;

        public static ConfigEntry<bool> UpdateNotification;

        public static void AllConfig(ConfigFile cfg)
        {
            // RefreshRate
            RefreshRate = cfg.Bind("RefreshRate", "RefreshRate", -1, "The target frame rate for XSOverlay rendering.\nHigher values improve responsiveness but increase CPU usage.\n- Set to 500 for unlimited.");
            OnlyHoverOverlay = cfg.Bind("RefreshRate", "OnlyHoverOverlay", true, "Only apply overriding refresh rate when hovering any Overlay.");
            OnlyInLayoutMod = cfg.Bind("RefreshRate", "OnlyInLayoutMod", true, "Only apply overriding refresh rate in Layout Mode.");

            // Cursor
            AlwayUpdateCursor = cfg.Bind("Cursor", "AlwayUpdateCursor", false, "Reduces Windows Cursor latency by sending position from the Pointer before the desktop frame is captured.\nWithout this, Windows Cursor often appears to lag one frame behind the Pointer position.");
            AlwaysHideCursor = cfg.Bind("Cursor", "AlwaysHideCursor", false, "Forcefully hide the system cursor in Window Capture Overlay.");
            PhysicalMouseDetector = cfg.Bind("Cursor", "PhysicalMouseDetector", true, "Release the Pointer control when physical mouse movement is detected.\nPointer click to regain control.");
            MouseSmoothSpeed = cfg.Bind("Cursor", "CursorSmoothSpeed", 10f, "Window overlay cursor smoothing.");

            // Pointer
            ActivePointerColor = cfg.Bind("Pointer", "ActivePointerColor", true, "Highlight the non-active hand's pointer in red for easier identification.");
            ActivePointerOpacity = cfg.Bind("Pointer", "ActivePointerOpacity", 50, "Set the opacity of the non-active hand's pointer.");
            ActivePointerWebView = cfg.Bind("Pointer", "ActivePointerWebView", true, "Apply the inactive Pointer feature to WebView Overlays such as Settings, Wrist, and others that is not Desktop or Window Capture.");
            PointerScaleMultiply = cfg.Bind("Pointer", "PointerScaleMultiply", 100, "Multiplier for the Pointer scale relative to the XSOverlay setting.");
            PointerDoubleClickDelay = cfg.Bind("Pointer", "PointerDoubleClickDelay", true, "Apply a Double Click Delay from XSOverlay setting to the Pointer itself, not just the cursor.");
            PointerActiveClick = cfg.Bind("Pointer", "PointerActiveClick", false, "Click non-active hand's pointer to become Active Hand and Mouse Click at the same time for two-hand clicking.");
            EmulateMouseClickAnimation = cfg.Bind("Pointer", "EmulateMouseClickAnimation", true, "Apply Pointer click animation for Input Method > Emulate Mouse.");
            LaserPointer = cfg.Bind("Pointer", "Laser Pointer", true, "Draw a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for more accurate targeting.");

            // Mouse Navigation
            MouseNavigation = cfg.Bind("Mouse Navigation", "MouseNavigation", false, "Custom keybindings for Mouse Forward/Back navigation.\nConfiguration by press \"Bindings\" tab in XSOverlay settings to open SteamVR bindings menu.\nEdit the Current Binding and add a button for \"MouseBack/MouseForward\".");
            MouseNavigationUseModifiedKey = cfg.Bind("Mouse Navigation", "MouseNavigationUseModifiedKey", false, "Use Alt+Left/Right keyboard shortcuts for navigation instead of mouse clicks.\nTargets the focused window instead of the hovered window.");

            DashboardNotification = cfg.Bind("Dashboard", "DashboardNotification", true, "Allow Notification to be displayed over SteamVR Dashboard.");
            DashboardPointer = cfg.Bind("Dashboard", "DashboardPointer", true, "Allow Pointer to be displayed over SteamVR Dashboard.");
            DashboardSettings = cfg.Bind("Dashboard", "DashboardSettings", true, "Allow Settings to be displayed over SteamVR Dashboard.");
            DashboardWindow = cfg.Bind("Dashboard", "DashboardWindow", false, "Allow Window Overlay to be displayed over SteamVR Dashboard.");
            DashboardWrist = cfg.Bind("Dashboard", "DashboardWrist", true, "Allow Wrist Overlay to be displayed over SteamVR Dashboard.");
            Dashboardkeyboard = cfg.Bind("Dashboard", "Dashboardkeyboard", false, "Allow Keyboard to be displayed over SteamVR Dashboard\n- Incompatible with Keyboard OSC mod.");

            // Optimization
            EfficiencyMode = cfg.Bind("Optimization", "EfficiencyMode", true, "Put XSOverlay in Windows Efficiency Mode to reduce CPU usage when not interacting with any overlays.");
            InactiveRefreshRate = cfg.Bind("Optimization", "InactiveRefreshRate", 15, "The target frame rate for XSOverlay rendering when in Efficiency Mode.");
            WristOverPosition = cfg.Bind("Optimization", "WristOverPosition", true, "Triple allow position radius of Wrist Overlay.");
            WebViewFix = cfg.Bind("Optimization", "WebViewFix", true, "Fix the WebView UI element unclickable for some reason.");
            WebViewWiderScroll = cfg.Bind("Optimization", "WebViewWiderScroll", true, "Make WebView scrollbar wider.");

            // About
            UpdateNotification = cfg.Bind("About", "UpdateNotifications", true, "Receive update notification when update are available.");
        }
    }
}
