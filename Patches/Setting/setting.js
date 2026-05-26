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
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.RefreshRate', name: 'Refresh Rate', description: 'The target frame rate for XSOverlay rendering.<br>Higher values improve responsiveness but increase CPU usage.<br><b>- Set to 500 for unlimited.</b>', default: <<HMDRefreshRate>>, options: [<<HMDRefreshRate>>, 500, 10], unit: 'FPS' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyHoverOverlay', name: 'Only Hover Overlay', description: 'Only apply overriding refresh rate when hovering any Overlay.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OnlyInLayoutMod', name: 'Only In Layout Mode', description: 'Only apply overriding refresh rate in Layout Mode.', default: true },
                ]
            },
            {
                name: 'Cursor', priority: 2, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwayUpdateCursor', name: 'Always Update', description: 'Reduces Windows Cursor latency by sending position from the Pointer before the desktop frame is captured.<br>Without this, Windows Cursor often appears to lag one frame behind the Pointer position.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.AlwaysHideCursor', name: 'Always Hide', description: 'Forcefully hide the system cursor in Capture Overlay.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PhysicalMouseDetector', name: 'Physical Mouse Detector', description: 'Release the Pointer control when physical mouse movement is detected.<br>Pointer click to regain control.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.MouseSmoothSpeed', name: 'Mouse Smoothing', description: 'Capture Overlay cursor smoothing.', default: 10.0, options: [0.1, 30.0, 0.1], unit: '%' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WindowsCursorPointer', name: 'Windows Cursor Pointer', description: 'Hide Capture Overlay Cursor and using Windows Cursor image as the Capture Overlay Pointer to mimic the SteamVR Dashboard.', default: true },
                ]
            },
            {
                name: 'Pointer', priority: 3, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.ActivePointerColor', name: 'Active Highlight', description: 'Highlight the non-active hand\'s pointer in red for easier identification.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.ActivePointerOpacity', name: 'Inactive Opacity', description: 'Set the opacity of the non-active hand\'s pointer.', default: 50, options: [0, 100, 10], unit: '%' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.ActivePointerWebView', name: 'Active WebViews', description: 'Apply the Inactive Pointer feature to WebView Overlays such as Settings, Wrist, and others that is not Desktop or Window Capture Overlay.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.PointerScaleMultiply', name: 'Scale Multiplier', description: 'Multiplier for the Pointer scale relative to the XSOverlay setting.', default: 100, options: [100, 1000, 50], unit: '%' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PointerDoubleClickDelay', name: 'Double Click Delay', description: 'Apply a Double Click Delay from XSOverlay setting to the Pointer itself, not just the cursor.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PointerActiveClick', name: 'Active Click', description: 'Click non-active hand\'s pointer to become Active Hand and Mouse Click at the same time for two-hand clicking.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.EmulateMouseClickAnimation', name: 'Emulate Mouse Click Animation', description: 'Apply Pointer click animation for Input Method > Emulate Mouse.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.LaserPointer', name: 'Laser', description: 'Draw a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for more accurate targeting.', default: true },
                ]
            },
            {
                name: 'Mouse Navigation', priority: 4, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigation', name: 'Enable', description: 'Custom keybindings for Mouse Forward/Back navigation.<br>Configuration by press Bindings tab in XSOverlay settings to open SteamVR bindings menu.<br>Edit the Current Binding and add a button for MouseBack/MouseForward.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.MouseNavigationUseModifiedKey', name: 'Use Alt+Left/Right', description: 'Use Alt+Left/Right keyboard shortcuts for navigation instead of mouse clicks.<br>Targets the focused window instead of the hovered window.', default: false },
                ]
            },
            {
                name: 'Dashboard Overlay', priority: 5, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardNotification', name: 'Dashboard Notification', description: 'Allow Notification to be displayed over SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardPointer', name: 'Dashboard Pointer', description: 'Allow Pointer to be displayed over SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardSettings', name: 'Dashboard Settings', description: 'Allow Settings to be displayed over SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWindow', name: 'Dashboard Window', description: 'Allow Window Overlay to be displayed over SteamVR Dashboard.', default: false },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.DashboardWrist', name: 'Dashboard Wrist', description: 'Allow Wrist Overlay to be displayed over SteamVR Dashboard.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.Dashboardkeyboard', name: 'Dashboard Keyboard', description: 'Allow Keyboard to be displayed over SteamVR Dashboard.<br><br>- Incompatible with Keyboard OSC mod.</b>', default: false },
                ]
            },
            {
                name: 'Optimization', priority: 6, settings: [
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.EfficiencyMode', name: 'Efficiency Mode', description: 'Put XSOverlay in Windows Efficiency Mode to reduce CPU usage when not interacting with any overlays.', default: true },
                    { type: Ui.ComponentType.Slider, id: 'XSOverlayTweak.InactiveRefreshRate', name: 'Inactive Refresh Rate', description: 'The target frame rate for XSOverlay rendering when in Efficiency Mode.', default: 15, options: [5, <<HMDRefreshRate>>, 1], unit: 'FPS' },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WristOverPosition', name: 'Wrist Over Position', description: 'Triple allow position radius of Wrist Overlay.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewFix', name: 'WebView Fix', description: 'Fix the WebView UI element unclickable for some reason.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.WebViewWiderScroll', name: 'WebView Wider Scroll', description: 'Make WebView scrollbar wider.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayCurveAutoRefresh', name: 'Overlay Curve Auto Refresh', description: 'Auto apply Overlay Curve setting to any behavior without grabbing Overlay to see a change. For example, when the Overlay Curve setting changes, Overlay Scaling and Overlay Spawning are affected.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.OverlayRollCurveFix', name: 'Overlay Roll Curve Fix', description: 'Fix the Capture Overlay to be invisible when the Curve and Angle are changing at the same time.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.PinBlockInputNonEditMode', name: 'Pin + Block Input Non Layout Mode', description: 'Non Layout Mode, Pin + Block Input Capture Overlay will no longer be available to hover.', default: true },
                ]
            },
            {
                name: 'About', priority: 7, settings: [
                    { type: Ui.ComponentType.Text, description: '<br>Version: <<Version>>'},
                    { type: Ui.ComponentType.Button, id: 'XSOverlayTweak.CheckForUpdate', name: 'Check for Updates', description: 'Check for the latest version of XSOverlay Tweak.', default: true },
                    { type: Ui.ComponentType.Button, id: 'XSOverlayTweak.OpenGitHub', name: 'Open GitHub', description: 'Visit the XSOverlay Tweak GitHub page.', default: true },
                    { type: Ui.ComponentType.Toggle, id: 'XSOverlayTweak.UpdateNotification', name: 'Update Notification', description: 'Receive update notification when update are available.', default: true },
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