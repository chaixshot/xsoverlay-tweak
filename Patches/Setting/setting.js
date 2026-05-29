if (window.XSOverlayTweak_Setting) return 'XSOverlayTweak_Setting already injected';
window.XSOverlayTweak_Setting = true;

function InjectKBOSCTab() {
    var scr = document.createElement('script');
    scr.type = 'module';
    scr.textContent = "import * as Ui from './_Shared/js/uiComponents.js'; (" + function (Ui) {
        // --- Configuration ---
        const CONFIG = {
            pageId: 'Page_XSOverlayTWEAK',
            pageName: 'XSOverlay Tweak',
            pageIcon: 'bi-tools',
            targetIndex: 0 // 0 for top, 1 for after General, etc.
        };

        const SECTIONS = [
            {
                name: 'Refresh Rate', priority: 1, settings: [
                    { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.RefreshRate', name: 'Refresh Rate', description: 'The target frame rate for XSOverlay rendering.<br>Higher values improve responsiveness but increase CPU usage.', default: <<HMDRefreshRate>>, options: [<<RefreshRateList>>] },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyHoverOverlay', name: 'Only Hover Overlay', description: 'Apply the custom Refresh Rate only when a Pointer is hovering over an Overlay.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyInLayoutMod', name: 'Only In Layout Mode', description: 'Apply the custom Refresh Rate only when Layout Mode is active.', default: true },
                ]
            },
            {
                name: 'Cursor', priority: 2, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwayUpdateCursor', name: 'Always Update', description: 'Reduces Windows Cursor latency by sending the position from the Pointer before the desktop frame is captured.<br>Without this, the Windows Cursor often appears to lag one frame behind the Pointer position.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwaysHideCursor', name: 'Always Hide', description: 'Forcefully hides the system Windows Cursor in Desktop and Window Capture Overlay.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PhysicalMouseDetector', name: 'Physical Mouse Detector', description: 'Relinquishes Pointer control when physical mouse movement is detected.<br>Pointer Click to regain control.', default: true },
                    { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.MouseSmoothSpeed', name: 'Mouse Smoothing', description: 'Adjusts the level of smoothing applied to the Windows Cursor within Capture Overlay.', default: 'Medium', options: ['Ultra Low', 'Very Low', 'Low', 'Medium', 'High', 'Very High'] },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WindowsCursorPointer', name: 'Windows Cursor Pointer', description: 'Hides the Capture Overlay Cursor and uses the Windows Cursor image as the Pointer to mimic the SteamVR Dashboard.', default: true },
                ]
            },
            {
                name: 'Pointer', priority: 3, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.ActivePointerWebView', name: 'Active WebViews', description: 'Applies the inactive Pointer features to WebView Overlay such as Settings, Wrist, and others.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.EmulateMouseClickAnimation', name: 'Emulate Mouse Click Animation', description: 'Enables the Pointer click visual animation for Input Method > Emulate Mouse.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.InactivePointerColor', name: 'Inactive Highlight', description: 'Highlights the inactive hand\'s Pointer in red for easier identification.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.InactivePointerOpacity', name: 'Inactive Opacity', description: 'Sets the opacity level for the inactive hand\'s Pointer.', default: 50, options: [0, 100, 10], unit: '%' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PointerActiveClick', name: 'Active Click', description: 'Clicking the inactive hand\'s Pointer makes it the Active Hand and performs a Mouse Click simultaneously for two-hand interaction.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PointerDoubleClickDelay', name: 'Double Click Delay', description: 'Applies the Double Click Delay from XSOverlay settings to the physical Pointer itself, not just the cursor.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PointerScaleMultiply', name: 'Scale Multiplier', description: 'Multiplier for the Pointer scale relative to the global XSOverlay setting.', default: 100, options: [100, 400, 25], unit: '%' },
                ]
            },
            {
                name: 'Mouse Navigation', priority: 4, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigation', name: 'Enable', description: 'Custom keybindings for Mouse Forward/Back navigation.<br>Configuration: Press \'Bindings\' tab in XSOverlay settings to open SteamVR bindings menu.<br>Edit the Current Binding and add a button for \'MouseBack/Forward\'.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigationUseModifiedKey', name: 'Use Alt+Left/Right', description: 'Use Alt+Left/Right keyboard shortcuts for navigation instead of Mouse Clicks.<br>Targets the focused window instead of the hovered window.', default: false },
                ]
            },
            {
                name: 'Dashboard Overlay', priority: 5, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardNotification', name: 'Dashboard Notification', description: 'Allows Notifications to be displayed over the SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardPointer', name: 'Dashboard Pointer', description: 'Allows the Pointer to be displayed and interactive over the SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardSettings', name: 'Dashboard Settings', description: 'Allows the Settings WebView Overlay to be displayed over the SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWindow', name: 'Dashboard Window', description: 'Allows Capture Overlay to be displayed over the SteamVR Dashboard.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWrist', name: 'Dashboard Wrist', description: 'Allows the Wrist Overlay to be displayed over the SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.Dashboardkeyboard', name: 'Dashboard Keyboard', description: 'Allows the Keyboard to be displayed over the SteamVR Dashboard.<br>- Incompatible with Keyboard OSC mod.', default: false },
                ]
            },
            {
                name: 'Optimization', priority: 6, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.EfficiencyMode', name: 'Efficiency Mode', description: 'Enables Windows Efficiency Mode for XSOverlay to reduce CPU usage when not interacting with any Overlay.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.InactiveRefreshRate', name: 'Inactive Refresh Rate', description: 'The target Refresh Rate for XSOverlay rendering when Efficiency Mode is active.<br>Very low value: the Layout Mode Toggle binding listener will miss some frames.', default: 15, options: [5, <<HMDRefreshRate>>, 1], unit: 'FPS' },
                ]
            },
            {
                name: 'Quality of Life', priority: 7, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DefaultCaptureOverlayTexture', name: 'Default Capture Overlay Texture', description: 'Initializes a Capture Overlay with a white texture to prevent new spawns from appearing invisible.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DoubleClickConfirm', name: 'Double Click Confirm', description: 'Ensures that a Double Click is always sent reliably when using Emulate Mouse mode.', default: true },
                    { type: Ui.ComponentType.Dropdown, id: 'XSOverlayTweak.LaserPointer', name: 'Laser', description: 'Draws a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for accurate targeting.', default: 'Enable Without Mouse Smooth', options: ['Disable', 'Enable Without Mouse Smooth', 'Enable With Mouse Smooth'] },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayCurveAutoRefresh', name: 'Overlay Curve Auto Refresh', description: 'Automatically applies Overlay Curve changes to all active behaviors. For example, when the Overlay Curve setting changes, Overlay Scaling and Overlay Spawning are affected', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PinBlockInputNonEditMode', name: 'Pin + Block Input Non Layout Mode', description: 'Blocks interaction with \'Pinned\' or \'Block Input\' Overlay unless Layout Mode is active.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PullTriggerClickThreshold', name: 'Pull Trigger Click Threshold', description: 'The Trigger pull threshold required to trigger a Left Click.<br>- Uses the Trigger Value from SteamVR Input.', default: 0.5, options: [0.1, 1.0, 0.1], unit: 'Unit' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PullTriggerPointerLock', name: 'Pull Trigger Pointer Lock', description: 'Locks the Pointer in place while the Trigger is held for easier double clicking.<br>- Uses the Trigger Value from SteamVR Input and Double Click Delay settings.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewWiderScroll', name: 'WebView Wider Scroll', description: 'Makes the WebView scrollbar wider for easier interaction.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WristOverPosition', name: 'Wrist Over Position', description: 'Increases the allowed positioning radius of the Wrist Overlay.', default: true },              
                ]
            },
            {
                name: 'Fix', priority: 8, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.LoadLayoutScaleFix', name: 'Load Layout Scale Fix', description: 'Ensures saved scale values are applied correctly when loading an Overlay Layout.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayRollCurveFix', name: 'Overlay Roll Curve Fix', description: 'Prevents an Overlay from turning invisible when curvature and rotation change simultaneously.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewFix', name: 'WebView Fix', description: 'Fixes an issue where certain WebView UI elements were not clickable.', default: true },
                ]
            },
            {
                name: 'About', priority: 9, settings: [
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

        // Determine insertion point for the button
        let referenceNodeForButton = null;
        if (CONFIG.targetIndex !== null && CONFIG.targetIndex < existingBtns.length) {
            referenceNodeForButton = existingBtns[CONFIG.targetIndex];
            sidebar.insertBefore(navBtn, referenceNodeForButton);
        }

        // Conditionally add a divider after the new button, mimicking existing sidebar behavior.
        // A divider is added after a button if there are other buttons following it.
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
            s.settings.forEach(set => {
                addSetting(section, set.type, set.id, set.name, set.description, set.default, set.options, set.unit);
            });
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

            // Update visual selection for all buttons in sidebar
            sidebar.querySelectorAll('.side-bar-button').forEach(b => {
                const isTarget = b === btn;
                b.classList.toggle('side-bar-button-selected', isTarget);
                if (b.firstElementChild) b.firstElementChild.classList.toggle('selected-icon', isTarget);
            });
        });
    }.toString() + ") (Ui);";
    document.body.appendChild(scr);
}

InjectKBOSCTab()

return 'XSOverlayTweak_Setting injected';