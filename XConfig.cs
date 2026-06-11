using BepInEx.Configuration;

namespace xsoverlay_tweak
{
    internal class XConfig
    {
        // RefreshRate
        public static ConfigEntry<string> RefreshRate;
        public static ConfigEntry<bool> OnlyHoverOverlay;
        public static ConfigEntry<bool> OnlyInLayoutMod;

        // Cursor
        public static ConfigEntry<bool> AlwaysUpdateCursor;
        public static ConfigEntry<bool> AlwaysHideCursor;
        public static ConfigEntry<bool> PhysicalMouseDetector;
        public static ConfigEntry<int> MouseSmoothSpeed;
        public static ConfigEntry<bool> WindowsCursorPointer;

        // Pointer
        public static ConfigEntry<bool> ActivePointerWebView;
        public static ConfigEntry<bool> EmulateMouseClickAnimation;
        public static ConfigEntry<bool> InactivePointerColor;
        public static ConfigEntry<int> InactivePointerOpacity;
        public static ConfigEntry<bool> PointerActiveClick;
        public static ConfigEntry<bool> PointerDoubleClickDelay;
        public static ConfigEntry<int> PointerScaleMultiply;

        // Mouse Navigation
        public static ConfigEntry<bool> MouseNavigation;
        public static ConfigEntry<bool> MouseNavigationUseModifiedKey;

        // Dashboard
        public static ConfigEntry<bool> DashboardNotification;
        public static ConfigEntry<bool> DashboardPointer;
        public static ConfigEntry<bool> DashboardSettings;
        public static ConfigEntry<bool> DashboardWindow;
        public static ConfigEntry<bool> DashboardWrist;
        public static ConfigEntry<bool> Dashboardkeyboard;

        // Haptic
        public static ConfigEntry<int> GrabHaptic;
        public static ConfigEntry<int> KeyboardKeyHaptic;
        public static ConfigEntry<int> KeyboardPressHaptic;
        public static ConfigEntry<int> OverlaySwapHaptic;
        public static ConfigEntry<bool> StickyKeyHaptic;
        public static ConfigEntry<int> PullTriggerPointerLockHaptic;
        public static ConfigEntry<bool> ToggleEditModeHaptic;
        public static ConfigEntry<int> WebViewHaptic;

        // Optimization
        public static ConfigEntry<bool> EfficiencyMode;
        public static ConfigEntry<int> InactiveRefreshRate;
        public static ConfigEntry<bool> uOSCThreadLoop;

        // Quality of Life
        public static ConfigEntry<bool> DefaultCaptureOverlayTexture;
        public static ConfigEntry<bool> DoubleClickConfirm;
        public static ConfigEntry<int> fpsVRSocket;
        public static ConfigEntry<int> LaserPointer;
        public static ConfigEntry<bool> OverlayCurveAutoRefresh;
        public static ConfigEntry<bool> PinBlockInputNonEditMode;
        public static ConfigEntry<float> PullTriggerClickThreshold;
        public static ConfigEntry<int> PullTriggerPointerLock;
        public static ConfigEntry<bool> WebViewWiderScroll;
        public static ConfigEntry<bool> WristOverPosition;

        // Track Space HMD Smooth
        public static ConfigEntry<bool> TrackSpaceHMDSmooth;
        public static ConfigEntry<int> TrackSpaceHMDAngleThreshold;
        public static ConfigEntry<int> TrackSpaceHMDDistThreshold;

        // Fix
        public static ConfigEntry<bool> CtrlKeyStickyFix;
        public static ConfigEntry<bool> CursorMovingInteractionFix;
        public static ConfigEntry<bool> HandleScrollingFix;
        public static ConfigEntry<bool> KeyboardControlButtonStateFix;
        public static ConfigEntry<bool> LoadLayoutScaleFix;
        public static ConfigEntry<bool> OverlayRollCurveFix;
        public static ConfigEntry<bool> WebViewFix;

        // Community Reqeust
        public static ConfigEntry<bool> HideBattery;
        public static ConfigEntry<bool> HideInvalidBattery;
        public static ConfigEntry<bool> LoadLayoutKeyboard;
        public static ConfigEntry<bool> MouseButtonSwap;
        public static ConfigEntry<bool> OverlayConfirmClose;
        public static ConfigEntry<bool> WindowToolbarGesture;
        public static ConfigEntry<bool> WindowToolbarKeyboard;
        public static ConfigEntry<bool> WristStateRestore;

        public static ConfigEntry<bool> UpdateNotification;

        public static void AllConfig(ConfigFile cfg)
        {
            // RefreshRate
            RefreshRate = cfg.Bind("RefreshRate", "RefreshRate", "Unknow", "The target frame rate for XSOverlay rendering.\nHigher values improve responsiveness but increase CPU usage.");
            OnlyHoverOverlay = cfg.Bind("RefreshRate", "OnlyHoverOverlay", true, "Apply the custom Refresh Rate only when a Pointer is hovering over an Overlay.");
            OnlyInLayoutMod = cfg.Bind("RefreshRate", "OnlyInLayoutMod", true, "Apply the custom Refresh Rate only when Layout Mode is active.");

            // Cursor
            AlwaysHideCursor = cfg.Bind("Cursor", "AlwaysHideCursor", false, "Forcefully hides the system Windows Cursor in Desktop and Window Capture Overlay.");
            AlwaysUpdateCursor = cfg.Bind("Cursor", "AlwaysUpdateCursor", false, "Reduces Windows Cursor latency by sending the position from the Pointer before the desktop frame is captured.\nWithout this, the Windows Cursor often appears to lag one frame behind the Pointer position.");
            MouseSmoothSpeed = cfg.Bind("Cursor", "MouseSmoothSpeed", 3, "Adjusts the level of smoothing applied to the Windows Cursor within Capture Overlay.");
            PhysicalMouseDetector = cfg.Bind("Cursor", "PhysicalMouseDetector", true, "Relinquishes Pointer control when physical mouse movement is detected.\nPointer Click to regain control.");
            WindowsCursorPointer = cfg.Bind("Cursor", "WindowsCursorPointer", true, "Hides the Capture Overlay Cursor and uses the Windows Cursor image as the Pointer to mimic the SteamVR Dashboard.");

            // Pointer
            ActivePointerWebView = cfg.Bind("Pointer", "ActivePointerWebView", true, "Applies the inactive Pointer features to WebView Overlay such as Settings, Wrist, and others.");
            EmulateMouseClickAnimation = cfg.Bind("Pointer", "EmulateMouseClickAnimation", true, "Enables the Pointer click visual animation for Input Method > Emulate Mouse.");
            InactivePointerColor = cfg.Bind("Pointer", "InactivePointerColor", true, "Highlights the inactive hand's Pointer in red for easier identification.");
            InactivePointerOpacity = cfg.Bind("Pointer", "InactivePointerOpacity", 50, "Sets the opacity level for the inactive hand's Pointer.");
            PointerActiveClick = cfg.Bind("Pointer", "PointerActiveClick", false, "Clicking the inactive hand's Pointer makes it the Active Hand and performs a Mouse Click simultaneously for two-hand interaction.");
            PointerDoubleClickDelay = cfg.Bind("Pointer", "PointerDoubleClickDelay", true, "Applies the Double Click Delay from XSOverlay settings to the physical Pointer itself, not just the cursor.");
            PointerScaleMultiply = cfg.Bind("Pointer", "PointerScaleMultiply", 100, "Multiplier for the Pointer scale relative to the global XSOverlay setting.");

            // Mouse Navigation
            MouseNavigation = cfg.Bind("MouseNavigation", "MouseNavigation", false, "Custom keybindings for Mouse Forward/Back navigation.\nConfiguration: Press 'Bindings' tab in XSOverlay settings to open SteamVR bindings menu.\nEdit the Current Binding and add a button for 'MouseBack/MouseForward'.");
            MouseNavigationUseModifiedKey = cfg.Bind("MouseNavigation", "MouseNavigationUseModifiedKey", false, "Use Alt+Left/Right keyboard shortcuts for navigation instead of Mouse Clicks.\nTargets the focused window instead of the hovered window.");

            // Dashboard
            DashboardNotification = cfg.Bind("Dashboard", "DashboardNotification", true, "Keeps Notifications visible while the SteamVR Dashboard is open.");
            DashboardPointer = cfg.Bind("Dashboard", "DashboardPointer", true, "Keeps Pointer visible and interactive while the SteamVR Dashboard is open.");
            DashboardSettings = cfg.Bind("Dashboard", "DashboardSettings", true, "Keeps Settings Overlay visible while the SteamVR Dashboard is open.");
            DashboardWindow = cfg.Bind("Dashboard", "DashboardWindow", false, "Keeps Capture Overlay visible while the SteamVR Dashboard is open.");
            DashboardWrist = cfg.Bind("Dashboard", "DashboardWrist", true, "Keeps Wrist Overlay visible while the SteamVR Dashboard is open.");
            Dashboardkeyboard = cfg.Bind("Dashboard", "Dashboardkeyboard", false, "Keeps Keyboard visible while the SteamVR Dashboard is open.");

            // Haptic
            GrabHaptic = cfg.Bind("Haptic", "GrabHaptic", 50, "Plays a haptic feedback when grab any Overlay.");
            KeyboardKeyHaptic = cfg.Bind("Haptic", "KeyboardKeyHaptic", 30, "Plays a haptic feedback when Pointer is hovering a Keyboard key.");
            KeyboardPressHaptic = cfg.Bind("Haptic", "KeyboardPressHaptic", 30, "Plays a haptic feedback when Pointer is pressing a Keyboard key.");
            OverlaySwapHaptic = cfg.Bind("Haptic", "OverlaySwapHaptic", 30, "Plays a haptic feedback when Pointer is switching Overlay.");
            StickyKeyHaptic = cfg.Bind("Haptic", "StickyKeyHaptic", true, "Plays a haptic feedback when keyboard key is sticky.");
            PullTriggerPointerLockHaptic = cfg.Bind("Haptic", "PullTriggerPointerLockHaptic", 30, "Plays a haptic feedback when Pull Trigger Pointer Lock.");
            ToggleEditModeHaptic = cfg.Bind("Haptic", "ToggleEditModeHaptic", true, "Plays a haptic feedback when toggle Layout Mode.");
            WebViewHaptic = cfg.Bind("Haptic", "WebViewHaptic", 30, "Plays a haptic feedback when Pointer is hovering a Keyboard key.");

            // Optimization
            EfficiencyMode = cfg.Bind("Optimization", "EfficiencyMode", true, "Enables Windows Efficiency Mode for XSOverlay to reduce CPU usage when not interacting with any Overlay.");
            InactiveRefreshRate = cfg.Bind("Optimization", "InactiveRefreshRate", 15, "The target Refresh Rate for XSOverlay rendering when not interacting with any Overlay.\nVery low value: the Layout Mode Toggle binding listener will miss some frames.");
            uOSCThreadLoop = cfg.Bind("Optimization", "uOSCThreadLoop", true, "Instead of connecting to OSC in the loop thread, connect to OSC server when new data is sent.");

            // Quality of Life
            DefaultCaptureOverlayTexture = cfg.Bind("QualityOfLife", "DefaultCaptureOverlayTexture", true, "Initializes a Capture Overlay with a white texture to prevent new spawns from appearing invisible.");
            DoubleClickConfirm = cfg.Bind("QualityOfLife", "DoubleClickConfirm", true, "Ensures that a Double Click is always sent reliably when using Emulate Mouse mode.");
            fpsVRSocket = cfg.Bind("QualityOfLife", "fpsVRSocket", 0, "Attaches the fpsVR overlay to a specific socket position of XSOverlay.");
            LaserPointer = cfg.Bind("QualityOfLife", "LaserPointer", 1, "Draws a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for accurate targeting.");
            OverlayCurveAutoRefresh = cfg.Bind("QualityOfLife", "OverlayCurveAutoRefresh", true, "Automatically applies Overlay Curve changes to all active behaviors. For example, when the Overlay Curve setting changes, Overlay Scaling and Overlay Spawning are affected");
            PinBlockInputNonEditMode = cfg.Bind("QualityOfLife", "BlockInputNonEditMode", true, "Blocks interaction with 'Pinned' or 'Block Input' Overlay unless Layout Mode is active.");
            PullTriggerClickThreshold = cfg.Bind("QualityOfLife", "PullTriggerClickThreshold", 0.5f, "The Trigger pull threshold required to trigger a Left Click.\n- Uses the Trigger Value from SteamVR Input.");
            PullTriggerPointerLock = cfg.Bind("QualityOfLife", "PullTriggerPointerLock", 1, "Locks the Pointer in place while the Trigger is held for easier double clicking.\n- Uses the Trigger Value from SteamVR Input and Double Click Delay settings.");
            WebViewWiderScroll = cfg.Bind("QualityOfLife", "WebViewWiderScroll", true, "Makes the WebView scrollbar wider for easier interaction.");
            WristOverPosition = cfg.Bind("QualityOfLife", "WristOverPosition", true, "Increases the allowed positioning radius of the Wrist Overlay.");

            // Track Space HMD Smooth
            TrackSpaceHMDSmooth = cfg.Bind("CommunityReqeust", "TrackSpaceHMDSmooth", true, "When Capture Overlay uses HMD Tracking Space, it will use Position Dampening and Rotation Dampening settings to smooth its movement.");
            TrackSpaceHMDAngleThreshold = cfg.Bind("CommunityReqeust", "TrackSpaceHMDAngleThreshold", 50, "HMD angle threshold to start moving the HMD-attached Capture Overlay.");
            TrackSpaceHMDDistThreshold = cfg.Bind("CommunityReqeust", "TrackSpaceHMDDistThreshold", 20, "HMD distance threshold to start moving the HMD-attached Capture Overlay.");

            // Fix
            CtrlKeyStickyFix = cfg.Bind("Fix", "CtrlKeyStickyFix", true, "Fix where double-tapping the Ctrl key does not sticky.");
            CursorMovingInteractionFix = cfg.Bind("Fix", "CursorMovingInteractionFix", true, "Fix where Windows cursor movement events fail to interact with elements. For example, hovering the cursor over the Windows taskbar displays a thumbnail preview, or dragging to move system tray icon.");
            HandleScrollingFix = cfg.Bind("Fix", "HandleScrollingFix", true, "Normalize stick scrolling speed by the HMD refresh rate and support horizontal scrolling.");
            KeyboardControlButtonStateFix = cfg.Bind("Fix", "KeyboardControlButtonStateFix", true, "Fix keyboard control button color not following the state when summoning.");
            LoadLayoutScaleFix = cfg.Bind("Fix", "LoadLayoutScaleFix", true, "Ensures saved scale values are applied correctly when loading an Overlay Layout.");
            OverlayRollCurveFix = cfg.Bind("Fix", "OverlayRollFlickerFix", true, "Prevents an Overlay from turning invisible when curvature and rotation change simultaneously.");
            WebViewFix = cfg.Bind("Fix", "WebViewFix", true, "Fixes an issue where certain WebView UI elements were not clickable.");

            // Community Reqeust
            HideBattery = cfg.Bind("CommunityReqeust", "HideBattery", false, "Hide Wrist Overlay battery information widget.");
            HideInvalidBattery = cfg.Bind("CommunityReqeust", "HideInvalidBattery", false, "Hide invalid battery device from Wrist Overlay.");
            LoadLayoutKeyboard = cfg.Bind("CommunityReqeust", "LoadLayoutKeyboard", true, "Layout will save the current keyboard state to the selected profile.");
            MouseButtonSwap = cfg.Bind("CommunityReqeust", "MouseButtonSwap", true, "Detecting the Windows setting 'Switch primary and secondary buttons' to auto-swap controller binding.");
            OverlayConfirmClose = cfg.Bind("CommunityReqeust", "OverlayConfirmClose", false, "Requires pressing the close overlay button three times to close.");
            WindowToolbarGesture = cfg.Bind("WindowToolbarGesture", "WindowToolbarGesture", true, "When hovering over the Window Toolbar, right-click to switch to previous Window or thumbstick scrolling the Window list.");
            WindowToolbarKeyboard = cfg.Bind("CommunityReqeust", "WindowToolbarKeyboard", false, "Add keyboard summon button to the Capture Overlay Toolbar.");
            WristStateRestore = cfg.Bind("CommunityReqeust", "WristStateRestore", true, "Restore the last Wrist Overlay state at launch.");

            // About
            UpdateNotification = cfg.Bind("About", "UpdateNotifications", true, "Displays a notification when a new version is available.");
        }
    }
}
