using BepInEx.Configuration;

namespace xsoverlay_tweak
{
    internal class XConfig
    {
        public static ConfigEntry<bool> EnableRefreshRate;
        public static ConfigEntry<int> RefreshRate;

        public static ConfigEntry<bool> AlwayUpdateCursor;
        public static ConfigEntry<bool> AlwaysHideCursor;

        public static ConfigEntry<bool> ActivePointerColor;
        public static ConfigEntry<int> ActivePointerOpacity;
        public static ConfigEntry<bool> ActivePointerWebView;
        public static ConfigEntry<int> PointerScaleMultiply;
        public static ConfigEntry<bool> PointerDoubleClickDelay;
        public static ConfigEntry<bool> PhysicalMouseDetector;

        public static ConfigEntry<bool> MouseNavigation;
        public static ConfigEntry<bool> MouseNavigationUseModifiedKey;

        public static ConfigEntry<bool> UpdateNotification;

        public static void AllConfig(ConfigFile cfg)
        {
            // RefreshRate
            EnableRefreshRate = cfg.Bind("RefreshRate", "EnableRefreshRate", false, "Overriding the XSOverlay render refresh rate.");
            RefreshRate = cfg.Bind("RefreshRate", "RefreshRate", -1, "The target frame rate for XSOverlay rendering.\nHigher values improve responsiveness but increase CPU usage.\nSet to 500 for unlimited.");

            // Cursor
            AlwayUpdateCursor = cfg.Bind("Cursor", "AlwayUpdateCursor", true, "Reduces cursor latency by sending cursor position data from the Pointer before the desktop frame is captured.\nWithout this, the cursor often appears to lag one frame behind the Pointer position.");
            AlwaysHideCursor = cfg.Bind("Cursor", "AlwaysHideCursor", false, "Forcefully hide the system cursor in Window Capture overlays.");
            PhysicalMouseDetector = cfg.Bind("Cursor", "PhysicalMouseDetector", true, "Release the Pointer control when physical mouse movement is detected.\nPointer click to regain control.");

            // Pointer
            ActivePointerColor = cfg.Bind("Pointer", "ActivePointerColor", true, "Highlight the non-active hand's pointer in red for easier identification.");
            ActivePointerOpacity = cfg.Bind("Pointer", "ActivePointerOpacity", 50, "Set the opacity of the non-active hand's pointer.");
            ActivePointerWebView = cfg.Bind("Pointer", "ActivePointerWebView", true, "Apply the inactive Pointer feature to WebView Overlays such as Settings, Wrist, and others that is not Desktop or Window Capture.");
            PointerScaleMultiply = cfg.Bind("Pointer", "PointerScaleMultiply", 100, "Multiplier for the Pointer scale relative to the XSOverlay setting.");
            PointerDoubleClickDelay = cfg.Bind("Pointer", "PointerDoubleClickDelay", true, "Apply a Double Click Delay from XSOverlay setting to the Pointer itself, not just the cursor.");

            // Mouse Navigation
            MouseNavigation = cfg.Bind("Mouse Navigation", "MouseNavigation", false, "Custom keybindings for Mouse Forward/Back navigation.\nConfiguration by press \"Bindings\" tab in XSOverlay settings to open SteamVR bindings menu.\nEdit the Current Binding and add a button for \"MouseBack/MouseForward\".");
            MouseNavigationUseModifiedKey = cfg.Bind("Mouse Navigation", "MouseNavigationUseModifiedKey", false, "Use Alt+Left/Right keyboard shortcuts for navigation instead of mouse clicks.\nThis targets the focused window instead of the hovered window.");

            // About
            UpdateNotification = cfg.Bind("About", "UpdateNotifications", true, "Receive update notification when update are available.");
        }
    }
}
