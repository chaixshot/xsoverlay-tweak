if (window.XSOverlayTweak_Setting) return 'XSOverlayTweak_Setting already injected';
window.XSOverlayTweak_Setting = true;

// --- Configuration ---
const CONFIG = {
    pageId: 'Page_XSOverlayTWEAK',
    pageName: 'XSOverlay Tweak',
    pageIcon: 'bi-tools',
    targetIndex: 0 
};

const SECTIONS = [
    {
        name: 'Refresh Rate', priority: 1, settings: [
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.RefreshRate', name: 'Refresh Rate', description: 'The target frame rate for XSOverlay rendering.<br>Higher FPS reduce latency but increase CPU usage.', default: '<<RefreshRate>>', options: [<<RefreshRateList>>] },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyHoverOverlay', name: 'Only Hover Overlay', description: 'Apply the custom Refresh Rate only when a Pointer is hovering over an Overlay.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyInLayoutMod', name: 'Only In Layout Mode', description: 'Apply the custom Refresh Rate only when Layout Mode is active.', default: true },
        ]
    },
    {
        name: 'Cursor', priority: 2, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwaysHideCursor', name: 'Always Hide', description: 'Forcefully hides the system Windows Cursor in Desktop and Window Capture Overlay.', default: false },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwaysUpdateCursor', name: 'Always Update', description: 'Reduces Windows Cursor latency by sending the position from the Pointer before the desktop frame is captured.<br>Without this, the Windows Cursor often appears to lag one frame behind the Pointer position.', default: false },
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.MouseSmoothSpeed', name: 'Mouse Smoothing', description: 'Adjusts the level of smoothing applied to the Windows Cursor within Capture Overlay.', default: 'Medium', options: ['Ultra Low', 'Very Low', 'Low', 'Medium', 'High', 'Very High'] },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PhysicalMouseDetector', name: 'Physical Mouse Detector', description: 'Relinquishes Pointer control when physical mouse movement is detected.<br>Pointer Click to regain control.', default: true },
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.WindowsCursorPointer', name: 'Windows Cursor Pointer', description: 'Hides the Capture Overlay Cursor and uses the Windows Cursor image as the Pointer to mimic the SteamVR Dashboard.', default: 'Enable', options: ['Disable', 'Enable', 'Enable + Animated (CPU Usage)'] },
        ]
    },
    {
        name: 'Pointer', priority: 3, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.ActivePointerWebView', name: 'Active WebViews', description: 'Applies the inactive Pointer features to WebView Overlay such as Settings, Wrist, and others.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.EmulateMouseClickAnimation', name: 'Emulate Mouse Click Animation', description: 'Enables the Pointer click visual animation for Input Method > Emulate Mouse.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.InactivePointerColor', name: 'Inactive Highlight', description: 'Highlights the inactive hand\'s Pointer in red for easier identification.', default: true },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.InactivePointerOpacity', name: 'Inactive Opacity', description: 'Sets the opacity level for the inactive hand\'s Pointer.', default: 50, options: [0, 100, 10], unit: '%' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PointerDoubleClickDelay', name: 'Double Click Delay', description: 'Applies the Double Click Delay from XSOverlay settings to the physical Pointer itself, not just the cursor.', default: true },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PointerScaleMultiply', name: 'Scale Multiplier', description: 'Multiplier for the Pointer scale relative to the global XSOverlay setting.', default: 100, options: [100, 400, 25], unit: '%' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.TwoHandedMode', name: 'Two Handed Mode', description: 'Allow both hands to become active hands at the same time to perform a Click simultaneously for two-hand interaction.', default: false },
        ]
    },
    {
        name: 'Wrist', priority: 4, settings: [
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.fpsVRSocket', name: 'fpsVR Socket', description: 'Attaches the fpsVR overlay to a specific socket position of XSOverlay.', default: "Disabled", options: ["Disabled", "Top", "Bottom", "Left", "Right"] },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.WristClipDistance', name: 'Wrist Clip Distance', description: 'Wrist Overlay auto hide based on head distance.', default: 60, options: [10, 100, 5], unit: 'Centimetre' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WristOverPosition', name: 'Wrist Over Position', description: 'Increases the allowed positioning radius of the Wrist Overlay.', default: true },                     
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WristStateRestore', name: 'Wrist State Restore', description: 'Restore the last Wrist Overlay state at launch.', default: true },
        ]
    },
    {
        name: 'Mouse Navigation', priority: 5, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigation', name: 'Enable', description: 'Custom keybindings for Mouse Forward/Back navigation.<br>Configuration: Press \'Bindings\' tab in XSOverlay settings to open SteamVR bindings menu.<br>Edit the Current Binding and add a button for \'MouseBack/Forward\'.', default: false },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigationUseModifiedKey', name: 'Use Alt+Left/Right', description: 'Use Alt+Left/Right keyboard shortcuts for navigation instead of Mouse Clicks.<br>Targets the focused window instead of the hovered window.', default: false },
        ]
    },
    {
        name: 'Dashboard Overlay', priority: 6, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardNotification', name: 'Dashboard Notification', description: 'Allows Notifications to be displayed over the SteamVR Dashboard.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardPointer', name: 'Dashboard Pointer', description: 'Allows the Pointer to be displayed and interactive over the SteamVR Dashboard.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardSettings', name: 'Dashboard Settings', description: 'Allows the Settings WebView Overlay to be displayed over the SteamVR Dashboard.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWindow', name: 'Dashboard Window', description: 'Allows Capture Overlay to be displayed over the SteamVR Dashboard.', default: false },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWrist', name: 'Dashboard Wrist', description: 'Allows the Wrist Overlay to be displayed over the SteamVR Dashboard.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.Dashboardkeyboard', name: 'Dashboard Keyboard', description: 'Allows the Keyboard to be displayed over the SteamVR Dashboard.', default: false },
        ]
    },
    {
        name: 'Haptic Feedback', priority: 7, settings: [
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.GrabHaptic', name: 'Grab', description: 'Plays a haptic feedback when grabbing any Overlay.', default: 50, options: [0, 100, 5], unit: '%' },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.KeyboardKeyHaptic', name: 'Keyboard Key', description: 'Plays a haptic feedback when Pointer is hovering a Keyboard key.', default: 30, options: [0, 100, 5], unit: '%' },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.KeyboardPressHaptic', name: 'Keyboard Press', description: 'Plays a haptic feedback when Pointer is pressing a Keyboard key.', default: 30, options: [0, 100, 5], unit: '%' },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.OverlaySwapHaptic', name: 'Overlay Swapping', description: 'Plays a haptic feedback when Pointer is switching Overlay.', default: 30, options: [0, 100, 5], unit: '%' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.StickyKeyHaptic', name: 'Sticky Key Haptic', description: 'Plays a haptic feedback when a sticky key is pressed.', default: true },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PullTriggerPointerLockHaptic', name: 'Pull Trigger Pointer Lock Haptic', description: 'Plays a haptic feedback when Pull Trigger Pointer Lock.', default: 30, options: [0, 100, 5], unit: '%' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.ToggleEditModeHaptic', name: 'Toggle Layout Mode', description: 'Plays a haptic feedback when toggle Layout Mode.', default: true },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.WebViewHaptic', name: 'WebView', description: 'Plays a haptic feedback when Pointer is hovering a Keyboard key.', default: 30, options: [0, 100, 5], unit: '%' },
        ]
    },
    {
        name: 'Optimization', priority: 8, settings: [
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.EfficiencyMode', name: 'Efficiency Mode', description: 'Enables Windows Efficiency Mode for XSOverlay to reduce CPU usage when not interacting with any Overlay.', default: 'Enable + Pinned Visible', options: ['Disable', 'Enable', 'Enable + Pinned Visible'] },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.InactiveRefreshRate', name: 'Inactive Refresh Rate', description: 'The target Refresh Rate for XSOverlay rendering when not interacting with any Overlay.<br>Very low value: the Layout Mode Toggle binding listener will miss some frames.', default: 15, options: [5, <<HMDRefreshRate>>, 1], unit: 'FPS' },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.uOSCThreadLoop', name: 'OSC Thread Loop', description: 'Instead of connecting to OSC in the loop thread, connect to the OSC server when new data is sent.', default: true },
        ]
    },
    {
        name: 'Quality of Life', priority: 9, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DefaultCaptureOverlayTexture', name: 'Default Capture Overlay Texture', description: 'Initializes a Capture Overlay with a white texture to prevent new spawns from appearing invisible.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DoubleClickConfirm', name: 'Double Click Confirm', description: 'Ensures that a Double Click is always sent reliably when using Emulate Mouse mode.', default: true },
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.LaserPointer', name: 'Laser', description: 'Draws a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for accurate targeting.', default: 'Enable Without Mouse Smooth', options: ['Disable', 'Enable Without Mouse Smooth', 'Enable With Mouse Smooth'] },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayCurveAutoRefresh', name: 'Overlay Curve Auto Refresh', description: 'Automatically applies Overlay Curve changes to all active behaviors. For example, when the Overlay Curve setting changes, Overlay Scaling and Overlay Spawning are affected', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PinBlockInputNonEditMode', name: 'Pin + Block Input Non Layout Mode', description: 'Blocks interaction with \'Pinned\' + \'Block Input\' Overlay unless Layout Mode is active.', default: true },
            { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PullTriggerClickThreshold', name: 'Pull Trigger Click Threshold', description: 'The Trigger pull threshold required to trigger a Left Click.<br>- Uses the Trigger Value from SteamVR Input.', default: 0.5, options: [0.1, 1.0, 0.1], unit: 'Unit' },
            { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.PullTriggerPointerLock', name: 'Pull Trigger Pointer Lock', description: 'Locks the Pointer in place while the Trigger is held for easier double clicking.<br>- Uses the Trigger Value from SteamVR Input and Double Click Delay settings.', default: "Only Capture Overlay", options: ['Disable', 'Only Capture Overlay', 'Capture + WebView Overlay'] },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewWiderScroll', name: 'WebView Wider Scroll', description: 'Makes the WebView scrollbar wider for easier interaction.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WindowsAccentColor', name: 'Windows Accent Color', description: 'Using Windows accent color as XSOverlay accent color.', default: true },
        ]
    },
    {
        name: 'Fix', priority: 10, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.CtrlKeyStickyFix', name: 'Ctrl Key Sticky', description: 'Fixes the issue where the Ctrl key is not sticky.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.CursorMovingInteractionFix', name: 'Cursor Moving Interaction', description: 'Fix where Windows cursor movement events fail to interact with elements. For example, hovering the cursor over the Windows taskbar displays a thumbnail preview, or dragging to move the system tray icon.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.HandleScrollingFix', name: 'Handle Scrolling', description: 'Normalize stick scrolling speed by the HMD refresh rate, support horizontal scrolling, support scrolling axis speed.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.KeyboardControlButtonStateFix', name: 'Keyboard Control Button State', description: 'Fix keyboard control button color not following the state when summoning.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.LoadLayoutScaleFix', name: 'Load Layout Scale', description: 'Ensures saved scale values are applied correctly when loading an Overlay Layout.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayGripSlipFix', name: 'Overlay Grip Slip', description: 'Prevents Overlay from dropping or slipping out of Grip when moving it too fast.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayRollCurveFix', name: 'Overlay Roll Curve', description: 'Prevents an Overlay from turning invisible when curvature and rotation change simultaneously.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewFrozenFix', name: 'WebView', description: 'Fixes an issue where certain WebView UI elements were not clickable.', default: true },
        ]
    },
    {
        name: 'Community Request', priority: 11, settings: [
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.HideBattery', name: 'Hide Battery', description: 'Hide the Wrist  Overlay battery information widget.', default: false },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.HideInvalidBattery', name: 'Hide Invalid Battery', description: 'Hide the invalid  battery device from Wrist Overlay.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.LoadLayoutKeyboard', name: 'Layout Keyboard State', description: 'Layout will save the current keyboard state to the selected profile.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseButtonSwap', name: 'Mouse Button Swap', description: 'Detect the Windows setting \'Switch primary and secondary buttons\' to auto-swap controller binding.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayAttachSmooth', name: 'Overlay Attach Smooth', description: 'When Capture Overlay is attached to the device, it will add more options to the Window Settings flyout to control Overlay movement behavior, using Position Dampening and Rotation Dampening settings to smooth its movement.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayConfirmClose', name: 'Overlay Confirm Close', description: 'Requires pressing the close overlay button three times to close.', default: false },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WindowToolbarGesture', name: 'Window Toolbar Gesture', description: 'When hovering over the Window Toolbar, right-click to switch to the previous Window or use thumbstick scrolling the Window list.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WindowToolbarKeyboard', name: 'Window Toolbar Keyboard', description: 'Add a keyboard  summon button to the Capture Overlay Toolbar.', default: false },
        ]
    },
    {
        name: 'About', priority: 12, settings: [
            { type: Ui.ComponentType.Text, description: '<br>Version: <<Version>>'},
            { type: Ui.ComponentType.Button, id: 'XSOverlayTweak.CheckForUpdate', name: 'Check for Updates', description: 'Check for the latest version of XSOverlay Tweak.', default: true },
            { type: Ui.ComponentType.Button, id: 'XSOverlayTweak.OpenGitHub', name: 'Open GitHub', description: 'Visit the XSOverlay Tweak GitHub page.', default: true },
            { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.UpdateNotification', name: 'Update Notification', description: 'Displays a notification when a new version is available.', default: true },
        ]
    }
];

const sidebar = document.querySelector('.side-bar-button-container');
const wrapper = document.querySelector('.page-wrapper');
if (!sidebar || !wrapper || document.getElementById(CONFIG.pageId)) return;

// --- Sidebar Navigation Button ---
const existingBtns = Array.from(sidebar.querySelectorAll('.side-bar-button'));

const navBtn = Ui.CreateElement(sidebar, 'button', ['side-bar-button']);
Ui.CreateElement(navBtn, 'i', ['side-bar-button-icon', 'theme-font-contrast', 'bi', CONFIG.pageIcon]);
const navLabel = Ui.CreateElement(navBtn, 'div', ['side-bar-button-text']);
navLabel.innerHTML = CONFIG.pageName;

let referenceNodeForButton = null;
if (CONFIG.targetIndex !== null && CONFIG.targetIndex < existingBtns.length) {
    referenceNodeForButton = existingBtns[CONFIG.targetIndex];
    sidebar.insertBefore(navBtn, referenceNodeForButton);
}

if (CONFIG.targetIndex !== null && CONFIG.targetIndex < existingBtns.length) {
    const newDivider = Ui.CreateElement(sidebar, 'div', ['sidebar-divider']);
    sidebar.insertBefore(newDivider, navBtn.nextSibling);
}

// --- Settings Page Layout ---
const pageRoot = Ui.CreateElement(wrapper, 'div', ['page-container', 'theme-dark']);
pageRoot.id = CONFIG.pageId;
pageRoot.style.cssText = 'position:absolute; opacity:0; pointer-events:none;';

const header = Ui.CreateElement(pageRoot, 'div', ['page-header']);
const headerText = Ui.CreateElement(header, 'div', ['page-header-text']);
headerText.innerHTML = CONFIG.pageName;

// --- Setting Builder Helper ---
const addSetting = (sectionObj, type, id, name, desc, defaultValue, opts, opts1) => {
    const setting = new Ui.Setting(type, name, desc, defaultValue, opts, opts1);
    setting.internalName = id;
    setting.sectionID = sectionObj.Name;

    const componentCreators = {
        [Ui.ComponentType.Toggle]: () => Ui.Toggle(setting, name, defaultValue, null, sectionObj.Background),
        [Ui.ComponentType.Button]: () => Ui.Button(setting, sectionObj.Background),
        [Ui.ComponentType.Slider]: () => {
            Ui.Slider(setting, name, defaultValue, opts, opts1, sectionObj.Background, 300);
            const el = document.getElementById(id);
            if (el) Ui.UpdateSliderUI(el, defaultValue);
        },
        [Ui.ComponentType.Dropdown]: () => {
            Ui.Dropdown(setting, name, defaultValue, opts, sectionObj.Background, 300);
            const el = document.getElementById(id);
            if (el) {
                const options = el.querySelectorAll('.selectopt');
                for (const opt of options) {
                    if (opt.getAttribute('internalName') === defaultValue || opt.getAttribute('index') === String(defaultValue)) {
                        opt.checked = true;
                        break;
                    }
                }
            }
        }
    };

    if (componentCreators[type]) componentCreators[type]();
    if (desc || type === Ui.ComponentType.Text) Ui.Description(sectionObj.Background, desc || '', id + '_Desc');

    Ui.Divider(sectionObj.Background, 'divider');
};

// --- Build Sections ---
SECTIONS.forEach(s => {
    const section = new Ui.Section(s.name, s.priority, pageRoot);
    const bg = section.Background;
    const header = bg.querySelector('.page-section-text');

    s.settings.forEach(set => {
        addSetting(section, set.type, set.id, set.name, set.description, set.default, set.options, set.unit);
    });

    bg.style.marginTop = '45px';

    // Make section collapsible by clicking its header
    if (header && s.name && !s.name.includes('hidden') && s.name !== '_' && s.name !== 'About') {
        header.style.cursor = 'pointer';
        header.style.display = 'flex';
        header.style.justifyContent = 'space-between';
        header.style.alignItems = 'center';
        header.style.userSelect = 'none';
        header.style.transition = 'font-weight 0.1s ease, transform 0.1s ease';
        header.style.transformOrigin = 'left center';

        header.addEventListener('mouseenter', () => {
            window.vuplex.postMessage('XSOverlayTweak-Haptic-Hover');
            header.style.fontWeight = 'bold';
            header.style.transform = 'scale(1.15)';
        });

        header.addEventListener('mouseleave', () => {
            header.style.fontWeight = '';
            header.style.transform = 'scale(1)';
        });

        const indicator = document.createElement('i');
        indicator.className = 'bi bi-chevron-down theme-font-contrast';
        indicator.style.transition = 'transform 0.2s ease';
        indicator.style.marginLeft = '5px';

        // Collapse all sections by default
        bg.classList.add('section-collapsed');
        indicator.style.transform = 'rotate(-90deg)';
        Array.from(bg.children).forEach(child => {
            if (child !== header) {
                child.style.display = 'none';
            }
        });

        header.appendChild(indicator);

        header.addEventListener('click', () => {
            const rectBefore = header.getBoundingClientRect().top;

            const isCollapsed = bg.classList.toggle('section-collapsed');
            indicator.style.transform = isCollapsed ? 'rotate(-90deg)' : '';
            Array.from(bg.children).forEach(child => {
                if (child !== header) {
                    child.style.display = isCollapsed ? 'none' : '';
                }
            });

            // Anti-expansion jump: maintain header position relative to viewpoint
            const rectAfter = header.getBoundingClientRect().top;
            pageRoot.scrollTop += (rectAfter - rectBefore);
        });
    }
});

// --- Navigation Logic ---
const switchPage = () => {
    wrapper.querySelectorAll('.page-container, .page-container-no-overflow').forEach(p => {
        if (p !== pageRoot) {
            p.style.animation = '0.3s ease fade-out forwards';
            p.style.pointerEvents = 'none';
        }
    });

    pageRoot.style.animation = '0.3s ease fade-in forwards';
    pageRoot.style.pointerEvents = 'auto';
};

sidebar.addEventListener('click', (e) => {
    const btn = e.target.closest('.side-bar-button');
    if (!btn) return;

    if (btn === navBtn) {
        switchPage();
    } else {
        pageRoot.style.animation = '0.3s ease fade-out forwards';
        pageRoot.style.pointerEvents = 'none';

        wrapper.querySelectorAll('.page-container, .page-container-no-overflow').forEach(p => {
            if (p !== pageRoot) {
                p.style.pointerEvents = 'auto';
            }
        });
    }

    sidebar.querySelectorAll('.side-bar-button').forEach(b => {
        const isTarget = b === btn;
        b.classList.toggle('side-bar-button-selected', isTarget);
        if (b.firstElementChild) b.firstElementChild.classList.toggle('selected-icon', isTarget);
    });
});

return 'XSOverlayTweak_Setting injected';