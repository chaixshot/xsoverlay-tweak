import * as Api from '../../../../_Shared/api.js';
import * as OpenVR from '../../../../_Shared/openvrapi.js';
import * as Common from '../js/common.js';
import * as Ui from '../js/uiComponents.js';

window.InitializeToolbar = InitializeToolbar;

//Wrist Elements
const Wrist = {
    Container: null,
    Background: null,
    BackgroundWidgetContainer: null,
    BatteryWidget: null,
    BatteryScrollRect: null,
    Clock: null,
    Date: null,
    TimeInVR: null,
    BackgroundCover: null,
    BackgroundGradient: null,
    WidgetContainer: null,
    LowerWidgetContainer: null,
    ClockContainer: null,
}

const LayoutWidget = {
    Container : null,
    MediaBackgroundContainer: null,
    MediaBackground: null,
    SelectorDropdown: null,
    ButtonContainer: null,
    SaveLayoutButton: null,
    LoadLayoutButton: null,
    DeleteLayoutButton: null,
    FavoriteLayoutButton: null,
    FavoriteIconText: null
}

const MediaPlayer = {
    Background: null,
    ControlsContainer: null,
    PlayPause: null,
    Next: null,
    Prev: null,
    PlayPauseIcon: null,
    InfoContainer: null,
    Track: null,
    Artist: null,
    Icon: null,
    AppIcon: null,
    AlbumArtBrightness: null,
    MediaThemeAccent: null,
    MediaThemeBackground: null
}

function PerformanceBarGraph(background, dataPoints) {
    this.background = background;
    this.dataPoints = dataPoints;
    this.currentSample = 0;
    this.maxSamples = 0;
}

function PerformanceDataBar(name, percent, extra, bar, barBackground, graph) {
    this.name = name;
    this.percent = percent;
    this.extra = extra;
    this.bar = bar;
    this.barBackground = barBackground;
    this.graph = graph;
}

function PerformanceSection(background, bars, halfBars, icon = null) {
    this.background = background;
    this.bars = bars;
    this.halfBars = halfBars;
    this.icon = icon;
}

const PerformanceMonitor = {
    Background: null,
    ContainerWrapper: null,
    Container: null,
    HorizontalContainer: null,
    GpuSection: null,
    CpuSection: null,
    FpsSection: null,
}

const CurrentMedia = Api.MediaObject;

const MiniToolbar = {
    Background: null,
    LayoutModeToggle: null,
    MediaPlayer: null,
    PerformanceStats: null,
}


let LastDevicePollData = null;
function DeviceTracker(parent, name, chargingIndicator, icon, battery, batteryBackground, timeEst) {
    this.name = name;
    this.parent = parent;
    this.chargingIndicator = chargingIndicator;
    this.icon = icon;
    this.battery = battery;
    this.batteryBackground = batteryBackground;
    this.timeEstimate = timeEst;
    this.timesPingedAtZero = 0;
}

var globalToolbarLookup = {
    "Create": "plus-circle-fill",
    "Recenter": "arrow-clockwise",
    "Keyboard": "keyboard-fill",
    "WindowSwitcher": "grid-3x2-gap-fill",
    "Layouts": "folder-fill",
    //"Applications": "rocket-takeoff-fill",
    "Settings": "gear-fill",
    "DeleteAll": "trash-fill"
}

var windowToolbarLookup = {
    "WindowSettings": "gear-fill",
    "WindowSelect": "aspect-ratio-fill",
    "WindowDelete": "x-circle-fill",
}

var toolbarTooltipLookup = {
    "Create": "Create",
    "Recenter": "Recenter",
    "Keyboard": "Keyboard",
    "WindowSwitcher": "GridView",
    "Layouts": "Layouts",
    //"Applications": "Applications",
    "Settings": "Settings",
    "DeleteAll": "DeleteAll",
    "WindowSettings": "Settings",
    "WindowSelect": "Windows",
    "WindowDelete": "Delete",
}

const svgns = "http://www.w3.org/2000/svg";
const progressBarWidth = 2;
var uiContainer;
var toolbar;
var DeviceTrackers = [];
var ToolbarButtons = [];
var ShowMediaPlayer = false;
var firstLoad = true;
var AutoMediaDetection = true;
var MediaThemeing = true;
var BatteryFontScale = 16;
var ShowingDetailedBatteryInformation = false;
var AlwaysShowDetailedInformation = false;
var ShowBatteryPercentageInsteadOfTimeEstimate = false;
var HideWristOverlay = true;
var IsLayoutMode = false;
var ShowLayoutsToolbar = false;
var WasShowingLayoutsToolbar = false;
var ShowPerformanceMonitor = false;
var OverlayHandleID = null;
var CurrentResolution = new Api.Resolution(670, 300);
var FavoriteLayout = -1;
var CurrentLayout = -1;
var AvailableLayouts = [];

var WristHorizontalOffset = 0

function SubscribeToApiEvents(bar) {
    Api.Client.Socket.addEventListener('open', () => {
        console.log("Successfully connected.");
        var eventList = [];
        if (bar == "GlobalToolbar") {
            eventList.push(Api.SubscriptionTag.Settings);
            eventList.push(Api.SubscriptionTag.DateAndTime);
            eventList.push(Api.SubscriptionTag.LayoutMode);
            eventList.push(Api.SubscriptionTag.MediaPlayer);
            eventList.push(Api.SubscriptionTag.DeviceInformation);

            Api.Send(Api.Commands.RequestLayoutInfo);
        }

        eventList.push(Api.SubscriptionTag.Theme);
        Api.Send(Api.Commands.SubscribeToEvents, JSON.stringify(eventList), null);
        Api.Send(Api.Commands.RequestOverlayIDs);
    });

    Api.Client.Socket.addEventListener('message', function message(data) {
        HandleMessages(data, bar);
    });

    Api.Client.Socket.addEventListener('close', () => {
        console.log(`${Api.ApiObject.sender} websocket was disconnected. Attempting reconnect.`);
        setTimeout(function () {
            CleanupUIState();
            Api.Connect(bar);
            SubscribeToApiEvents(bar);
            console.log("Reconnecting...");
        }, 1000);
    });
}

function CleanupUIState() {
    if (ShowPerformanceMonitor) {
        PerformanceMonitor.Background.style.opacity = 0;
        ShowPerformanceMonitor = false;
        CheckUIResolution();
    }
}

function InitializeToolbar(bar) {
    toolbar = document.getElementsByClassName("toolbar")[0];
    uiContainer = document.getElementById('uiContainer');
    uiContainer.classList.add('ui-container');
    
    uiContainer.appendChild(toolbar);
    InitializeUI(bar);

    Api.Connect(bar);
    SubscribeToApiEvents(bar);
}

function InitializeUI(bar) {
    CreateToolbarButtons(bar);

    if (bar === "GlobalToolbar") {
        CreateWristElements(toolbar.offsetWidth);
        CreateMiniToolbar(bar);
        CreateMediaPlayerElements();
        CreatePerformanceMonitorElements();
        CreateLayoutWidgetElements();
    }
}

function CreateLayoutWidgetElements() {
    let buttonSize = 50;
    let iconSize = 28;

    LayoutWidget.Container = Ui.CreateElement(Wrist.Container, Ui.HtmlType.div, ['layouts-widget', 'no-select']);
    LayoutWidget.MediaBackgroundContainer = Ui.CreateElement(LayoutWidget.Container, Ui.HtmlType.div, ['layouts-widget-background-container']);
    LayoutWidget.MediaBackground = Ui.CreateElement(LayoutWidget.MediaBackgroundContainer, Ui.HtmlType.image, ['layouts-widget-background']);
    
    LayoutWidget.SelectorDropdown = Ui.Dropdown2(LayoutWidget.Container, 'down', ['1', '2', '3', '4', '5', '6', '7', '8'], '8', 35, 4.75, 190, buttonSize, 1, true);

    LayoutWidget.ButtonContainer = Ui.CreateElement(LayoutWidget.Container, Ui.HtmlType.div, ['toolbar', 'theme-mid'], new Ui.uiTransform(278, buttonSize, 1));
    LayoutWidget.ButtonContainer.style.borderRadius = `${16}px`;

    LayoutWidget.FavoriteLayoutButton = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(0, buttonSize, 1));
    LayoutWidget.FavoriteIconText = Ui.CreateElement(LayoutWidget.FavoriteLayoutButton, Ui.HtmlType.div, ['bi-star']);
    LayoutWidget.FavoriteLayoutButton.style.borderRadius = `${16}px`;
    LayoutWidget.FavoriteLayoutButton.style.fontSize = `${iconSize}px`;
    LayoutWidget.FavoriteLayoutButton.appendChild(LayoutWidget.FavoriteIconText);

    let divider = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.div, ['glass-divider-vertical'], new Ui.uiTransform(2.5, buttonSize / 1.5, 1));
    LayoutWidget.SaveLayoutButton = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(0, buttonSize, 1));
    let saveText = Ui.CreateElement(LayoutWidget.SaveLayoutButton, Ui.HtmlType.div, ['bi-feather']);
    LayoutWidget.SaveLayoutButton.style.borderRadius = `${16}px`;
    LayoutWidget.SaveLayoutButton.style.fontSize = `${iconSize}px`;
    LayoutWidget.SaveLayoutButton.appendChild(saveText);
    
    let divider1 = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.div, ['glass-divider-vertical'], new Ui.uiTransform(2.5, buttonSize / 1.5, 1));
    LayoutWidget.LoadLayoutButton = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(0, buttonSize, 1));
    let loadText = Ui.CreateElement(LayoutWidget.LoadLayoutButton, Ui.HtmlType.div, ['bi-arrow-clockwise']);
    LayoutWidget.LoadLayoutButton.style.borderRadius = `${16}px`;
    LayoutWidget.LoadLayoutButton.style.fontSize = `${iconSize}px`;
    LayoutWidget.LoadLayoutButton.appendChild(loadText);
    
    let divider2 = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.div, ['glass-divider-vertical'], new Ui.uiTransform(2.5, buttonSize / 1.5, 1));
    LayoutWidget.DeleteLayoutButton = Ui.CreateElement(LayoutWidget.ButtonContainer, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(0, buttonSize, 1));
    let clearText = Ui.CreateElement(LayoutWidget.DeleteLayoutButton, Ui.HtmlType.div, ['bi-x-lg']);
    LayoutWidget.DeleteLayoutButton.style.borderRadius = `${16}px`;
    LayoutWidget.DeleteLayoutButton.style.fontSize = `${iconSize}px`;
    LayoutWidget.DeleteLayoutButton.appendChild(clearText);

    LayoutWidget.SelectorDropdown.onChange((val) => {
        console.log("Selected Layout : " + val);
        let activeLayout = Math.max(0, parseInt(val) - 1);
        CurrentLayout = activeLayout;
        Api.Send(Api.Commands.LoadLayout, null, activeLayout);
        Api.Send(Api.Commands.RequestLayoutInfo);
        UpdateLayoutToolbarButtonStates();
    });
    
    LayoutWidget.LoadLayoutButton.addEventListener("click", function (e) {
        setTimeout(function () { LayoutWidget.LoadLayoutButton.blur(); }, 150); //deselect button
        let activeLayout = Math.max(0, parseInt(LayoutWidget.SelectorDropdown.getValue() - 1));
        Api.Send(Api.Commands.LoadLayout, null, activeLayout);
        Api.Send(Api.Commands.RequestLayoutInfo);
        UpdateLayoutToolbarButtonStates();
        e.preventDefault;
    });

    LayoutWidget.SaveLayoutButton.addEventListener("click", function (e) {
        setTimeout(function () { LayoutWidget.SaveLayoutButton.blur(); }, 150); //deselect button
        let activeLayout = Math.max(0, parseInt(LayoutWidget.SelectorDropdown.getValue() - 1));
        Api.Send(Api.Commands.SaveLayout, null, activeLayout);
        Api.Send(Api.Commands.RequestLayoutInfo);
        UpdateLayoutToolbarButtonStates();
        e.preventDefault;
    });

    LayoutWidget.DeleteLayoutButton.addEventListener("click", function (e) {
        setTimeout(function () { LayoutWidget.DeleteLayoutButton.blur(); }, 150); //deselect button
        let activeLayout = Math.max(0, parseInt(LayoutWidget.SelectorDropdown.getValue() - 1));
        Api.Send(Api.Commands.DeleteLayout, null, activeLayout);
        Api.Send(Api.Commands.RequestLayoutInfo);
        UpdateLayoutToolbarButtonStates();
        e.preventDefault;
    });

    LayoutWidget.FavoriteLayoutButton.addEventListener("click", function (e) {
        setTimeout(function () { LayoutWidget.FavoriteLayoutButton.blur(); }, 150); //deselect button
        let activeLayout = Math.max(0, parseInt(LayoutWidget.SelectorDropdown.getValue() - 1));
        FavoriteLayout = activeLayout;
        UpdateLayoutToolbarButtonStates();
        Api.Send(Api.Commands.FavoriteLayout, null, activeLayout);
        Api.Send(Api.Commands.RequestLayoutInfo);
        UpdateLayoutToolbarButtonStates();
        e.preventDefault;
    });

    LayoutWidget.LoadLayoutButton.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "Load", true);
    });

    LayoutWidget.LoadLayoutButton.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });
    
    LayoutWidget.SaveLayoutButton.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "Save", true);
    });

    LayoutWidget.SaveLayoutButton.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });

    LayoutWidget.DeleteLayoutButton.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "Delete", true);
    });

    LayoutWidget.DeleteLayoutButton.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });
    
    LayoutWidget.FavoriteLayoutButton.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "Favorite", true);
    });
    
    LayoutWidget.FavoriteLayoutButton.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });
    
    OnToggleLayoutToolbar();
}

function CreateToolbarButtons(bar) {
    let barSelection = bar === "GlobalToolbar" ? globalToolbarLookup : windowToolbarLookup;
    let barLength = Object.keys(barSelection).length;
    let currentIndex = 0;
    Object.keys(barSelection).forEach(key => {
        let isRightSideButton = currentIndex == barLength - 1;
        let isLeftSideButton = currentIndex == 0;

        if (!isLeftSideButton) {
            let divider = Ui.CreateElement(toolbar, Ui.HtmlType.div, ['toolbar-divider'], new Ui.uiTransform(0, 0, 1));
        }

        let button = document.createElement("button");
        button.id = Object.keys(barSelection)[currentIndex];
        button.className = isRightSideButton ? "button buttonR" : (isLeftSideButton ? "button buttonL" : "button");

        let imgContainer = document.createElement("div");
        imgContainer.classList.add('button-image-container');
        let icon = document.createElement(Ui.HtmlType.img);
        icon.classList.add(`bi-${barSelection[key]}`);
        // icon.setAttribute("mIcon", `${barSelection[key]}`);

        button.appendChild(imgContainer);
        imgContainer.appendChild(icon);
        toolbar.appendChild(button);

        button.addEventListener("click", function (e) {
            setTimeout(function () { button.blur(); }, 150); //deselect button

            if (key === "Layouts") {
                ShowLayoutsToolbar = !ShowLayoutsToolbar;
                OnToggleLayoutToolbar();
            }
            
            Api.Send(`${key}`, null, null);
            e.preventDefault;
        });

        button.addEventListener("mouseenter", function (e) {
            Api.Send(Api.Commands.ShowTooltip, toolbarTooltipLookup[key], true);
        });

        button.addEventListener("mouseleave", function (e) {
            Api.Send(Api.Commands.ShowTooltip, null, false);
        });

        ToolbarButtons.push(button);
        currentIndex++;
    });
}

function CreateMiniToolbar(bar) {
    let buttonSize = 90;
    let container = Ui.CreateElement(Wrist.BackgroundWidgetContainer, Ui.HtmlType.div, ['toolbar-mini', 'glass-backdrop'], new Ui.uiTransform(0, buttonSize, 1));
    MiniToolbar.Background = container;

    MiniToolbar.LayoutModeToggle = Ui.CreateElement(container, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(buttonSize, buttonSize, 1));
    MiniToolbar.LayoutModeToggle.style.borderRadius = `25px`;
    let div0 = Ui.CreateElement(container, Ui.HtmlType.div, ['glass-divider-vertical']);
    
    MiniToolbar.MediaPlayer = Ui.CreateElement(container, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(buttonSize, buttonSize, 1));
    MiniToolbar.MediaPlayer.style.borderRadius = `25px`;
    let div1 = Ui.CreateElement(container, Ui.HtmlType.div, ['glass-divider-vertical']);
    
    MiniToolbar.PerformanceStats = Ui.CreateElement(container, Ui.HtmlType.button, ['button', 'button-image-container', 'theme-mid'], new Ui.uiTransform(buttonSize, buttonSize, 1));
    MiniToolbar.PerformanceStats.style.borderRadius = `25px`;

    let layoutIcon = Ui.CreateElement(MiniToolbar.LayoutModeToggle, Ui.HtmlType.div, ['bi-layers-fill', 'theme-font-contrast']);
    let mediaPlayerIcon = Ui.CreateElement(MiniToolbar.MediaPlayer, Ui.HtmlType.div, ['bi-music-note-list', 'theme-font-contrast']);
    let perfStatsIcon = Ui.CreateElement(MiniToolbar.PerformanceStats, Ui.HtmlType.div, ['bi-bar-chart-line-fill', 'theme-font-contrast']);

    MiniToolbar.LayoutModeToggle.addEventListener("click", function (e) {
        setTimeout(function () { MiniToolbar.LayoutModeToggle.blur(); }, 150); //deselect button
        Api.Send('ToggleLayoutMode', null, null);
        e.preventDefault;
    });

    MiniToolbar.MediaPlayer.addEventListener("click", function (e) {
        setTimeout(function () { MiniToolbar.MediaPlayer.blur(); }, 150); //deselect button
        OnToggleMediaPlayer();
        e.preventDefault;
    });

    MiniToolbar.PerformanceStats.addEventListener("click", function (e) {
        setTimeout(function () { MiniToolbar.PerformanceStats.blur(); }, 150); //deselect button
        Api.Send('TogglePerformanceStats', null, null);
        e.preventDefault;
    });

    MiniToolbar.LayoutModeToggle.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "LayoutMode", true);
    });

    MiniToolbar.MediaPlayer.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "MediaPlayer", true);
    });

    MiniToolbar.PerformanceStats.addEventListener("mouseenter", function (e) {
        Api.Send(Api.Commands.ShowTooltip, "Performance", true);
    });

    MiniToolbar.LayoutModeToggle.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });

    MiniToolbar.MediaPlayer.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });

    MiniToolbar.PerformanceStats.addEventListener("mouseleave", function (e) {
        Api.Send(Api.Commands.ShowTooltip, null, false);
    });
}

function CreateWristElements(width) {
    WristHorizontalOffset = (width - 500) / 2;
    uiContainer.style.setProperty('--wrist-horizontal-offset', `${WristHorizontalOffset}px`);
    
    Wrist.Container = Ui.CreateElement(uiContainer, Ui.HtmlType.div, ['wrist-container'], new Ui.uiTransform(500, 0, 1));
    Wrist.Container.style.left = `${WristHorizontalOffset}px`;
    
    Wrist.Background = Ui.CreateElement(Wrist.Container, Ui.HtmlType.div, ['rounded-background', 'theme-dark'], new Ui.uiTransform(500, 0, 1));

    var backgroundImageContainer = Ui.CreateElement(Wrist.Background, Ui.HtmlType.div, ['position-absolute', 'background-image-container']);
    Wrist.BackgroundCover = Ui.CreateElement(backgroundImageContainer, Ui.HtmlType.image, ['position-absolute', 'background-image']);

    Wrist.BackgroundGradient = Ui.CreateElement(backgroundImageContainer, Ui.HtmlType.div, ['position-absolute', 'background-image-fader']);
    Wrist.BackgroundCover.addEventListener('load', function () {
        Vibrant.from(Wrist.BackgroundCover).getPalette(function (err, palette) {
            var paletteToUse = palette.DarkMuted == null ? palette.LightVibrant : palette.DarkMuted;
            var secondPaletteToUse = palette.Vibrant == null ? palette.Muted : palette.Vibrant;

            MediaPlayer.MediaThemeBackground = `${rgb(paletteToUse._rgb)}`;
            MediaPlayer.MediaThemeAccent = `${rgb(secondPaletteToUse._rgb)}`;

            var rgbColor = secondPaletteToUse._rgb;
            MediaPlayer.AlbumArtBrightness = Math.round(((parseInt(rgbColor[0]) * 299) +
                (parseInt(rgbColor[1]) * 587) +
                (parseInt(rgbColor[2]) * 114)) / 1000);

            if (MediaThemeing && (AutoMediaDetection || ShowMediaPlayer)) {
                OnThemeMediaPlayer(true);
            }
        });
    });

    Wrist.BackgroundWidgetContainer = Ui.CreateElement(Wrist.Background, Ui.HtmlType.div, ['container-widget-background']);
    Wrist.ClockContainer = Ui.CreateElement(Wrist.BackgroundWidgetContainer, Ui.HtmlType.div, ['clock-container']);
    Wrist.Clock = Ui.CreateElement(Wrist.ClockContainer, Ui.HtmlType.div, ['clock', 'noselect']);
    Wrist.Date = Ui.CreateElement(Wrist.ClockContainer, Ui.HtmlType.div, ['date', 'noselect']);
    Wrist.TimeInVR = Ui.CreateElement(Wrist.ClockContainer, Ui.HtmlType.div, ['time-in-vr', 'noselect']);

    Wrist.WidgetContainer = Ui.CreateElement(Wrist.Background, Ui.HtmlType.div, ['battery-widget-wrapper']);
    Wrist.LowerWidgetContainer = Ui.CreateElement(Wrist.WidgetContainer, Ui.HtmlType.div, ['battery-widget-wrapper']);
    
    Wrist.BatteryWidget = Ui.CreateElement(Wrist.LowerWidgetContainer, Ui.HtmlType.div, ['battery-widget']);
    Wrist.BatteryScrollRect = Ui.CreateElement(Wrist.BatteryWidget, Ui.HtmlType.div, ['battery-scroll-rect']);

    Wrist.BatteryWidget.addEventListener('mouseenter', () => {
        if (AlwaysShowDetailedInformation) return;

        ShowingDetailedBatteryInformation = true;
        DeviceTrackers.forEach((device) => {
            device.timeEstimate.style.animation = 'show-time-left 200ms forwards';
            device.batteryBackground.style.animation = 'raise-battery-bar 200ms forwards'
            device.icon.style.animation = 'raise-battery-icon 200ms forwards'
        });
    });

    Wrist.BatteryWidget.addEventListener('mouseleave', () => {
        if (AlwaysShowDetailedInformation) return;

        ShowingDetailedBatteryInformation = false;
        DeviceTrackers.forEach((device) => {
            device.timeEstimate.style.animation = 'hide-time-left 200ms forwards';
            device.batteryBackground.style.animation = 'lower-battery-bar 200ms forwards'
            device.icon.style.animation = 'lower-battery-icon 200ms forwards'
        });
    });

    Wrist.BatteryWidget.addEventListener('click', () => {
        ShowBatteryPercentageInsteadOfTimeEstimate = !ShowBatteryPercentageInsteadOfTimeEstimate;
        if (LastDevicePollData != null) {
            OnUpdateDevices(LastDevicePollData);
        }
    });
}

function CreateMediaPlayerElements() {
    MediaPlayer.Background = Ui.CreateElement(Wrist.WidgetContainer, Ui.HtmlType.div, ['media-widget', 'glass-backdrop']);
    MediaPlayer.Background.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) hide-media-player forwards';

    MediaPlayer.InfoContainer = Ui.CreateElement(MediaPlayer.Background, Ui.HtmlType.div, ['media-widget-info-container', 'noselect']);
    MediaPlayer.Track = Ui.CreateElement(MediaPlayer.InfoContainer, Ui.HtmlType.div, ['media-widget-track', 'theme-font-contrast']);
    MediaPlayer.Artist = Ui.CreateElement(MediaPlayer.InfoContainer, Ui.HtmlType.div, ['media-widget-artist', 'theme-font-contrast']);
    MediaPlayer.Icon = Ui.CreateElement(MediaPlayer.Background, Ui.HtmlType.image, ['media-widget-icon', 'noselect']);

    MediaPlayer.ControlsContainer = Ui.CreateElement(MediaPlayer.Background, Ui.HtmlType.div, ['media-widget-controls']);

    MediaPlayer.Prev = Ui.CreateElement(MediaPlayer.ControlsContainer, Ui.HtmlType.button, ['media-widget-button', 'button-image-container']);
    var prevIcon = Ui.CreateElement(MediaPlayer.Prev, Ui.HtmlType.div, ['bi-skip-start-fill', 'theme-font-contrast']);

    MediaPlayer.PlayPause = Ui.CreateElement(MediaPlayer.ControlsContainer, Ui.HtmlType.button, ['media-widget-button', 'button-image-container']);
    MediaPlayer.PlayPauseIcon = Ui.CreateElement(MediaPlayer.PlayPause, Ui.HtmlType.div, ['bi-play-fill', 'theme-font-contrast']);

    MediaPlayer.Next = Ui.CreateElement(MediaPlayer.ControlsContainer, Ui.HtmlType.button, ['media-widget-button', 'button-image-container']);
    var nextIcon = Ui.CreateElement(MediaPlayer.Next, Ui.HtmlType.div, ['bi-skip-end-fill', 'theme-font-contrast']);

    MediaPlayer.PlayPause.addEventListener("click", function (e) {
        setTimeout(function () { MediaPlayer.PlayPause.blur(); }, 150); //deselect button

        Api.Send('MediaPlayPause', null, null);
        e.preventDefault;
    });


    MediaPlayer.Prev.addEventListener("click", function (e) {
        setTimeout(function () { MediaPlayer.Prev.blur(); }, 150); //deselect button

        Api.Send('MediaPrevious', null, null);
        e.preventDefault;
    });


    MediaPlayer.Next.addEventListener("click", function (e) {
        setTimeout(function () { MediaPlayer.Next.blur(); }, 150); //deselect button

        Api.Send('MediaNext', null, null);
        e.preventDefault;
    });
}

function CreatePerformanceBar(id, parent, barName, showMiddlePoint = false) {
    let container = Ui.CreateElement(parent, Ui.HtmlType.div, ['performance-bar-wrapper']);
    container.id = id;

    let itemContainer = Ui.CreateElement(container, Ui.HtmlType.div, ['performance-bar-item-container']);

    let textContainer = Ui.CreateElement(itemContainer, Ui.HtmlType.div, ['performance-bar-text-container']);
    let name = Ui.CreateElement(textContainer, Ui.HtmlType.div, ['performance-bar-text-name', 'theme-font-contrast']);
    name.innerHTML = barName;

    let extra = Ui.CreateElement(textContainer, Ui.HtmlType.div, ['performance-bar-text-extra', 'theme-font-contrast']);
    extra.innerHTML = '--'

    let barBackground = Ui.CreateElement(itemContainer, Ui.HtmlType.div, ['performance-bar-percent-background']);
    let bar = Ui.CreateElement(barBackground, Ui.HtmlType.div, ['performance-bar-percent-foreground']);

    if (showMiddlePoint) {
        let middlePoint = Ui.CreateElement(barBackground, Ui.HtmlType.div, ['performance-bar-text-middle-point', 'theme-font-contrast']);
        middlePoint.innerHTML = '- | +'
        middlePoint.style.marginLeft = `calc(50% - ${middlePoint.offsetWidth * 0.5}px)`;
    }

    let percentage = Ui.CreateElement(barBackground, Ui.HtmlType.div, ['performance-bar-text-percentage', 'theme-font-contrast']);
    percentage.innerHTML = "--%";

    return new PerformanceDataBar(name, percentage, extra, bar, barBackground, null); // TODO:: figure out the graphing.
}

function CreatePerformanceMonitorSection(id, parent) {
    let background = Ui.CreateElement(parent, Ui.HtmlType.button, ['performance-hardware-background', 'no-select']);
    background.id = id;
    background.addEventListener("click", function (e) {
        setTimeout(function () { background.blur(); }, 50); //deselect button
        e.preventDefault;
    });

    let containerContainer = Ui.CreateElement(background, Ui.HtmlType.div, ['performance-hardware-background-container-wrapper']);
    let container = Ui.CreateElement(containerContainer, Ui.HtmlType.div, ['performance-hardware-container']);
    let halfbarContainer = Ui.CreateElement(containerContainer, Ui.HtmlType.div, ['performance-hardware-halfbar-container']);

    let bars = new Map();
    let halfBars = new Map();
    switch (id) {
        case "framedata":
            bars.set("framerate", CreatePerformanceBar("framerate", container, "FPS"));
            bars.set("gpuFrametime", CreatePerformanceBar("gpuFrametime", halfbarContainer, "GPU"));
            bars.set("cpuFrametime", CreatePerformanceBar("cpuFrametime", halfbarContainer, "CPU"));
            break;

        case "gpu":
            bars.set("load", CreatePerformanceBar("load", container, "GPU"));
            bars.set("mem", CreatePerformanceBar("vram", container, "VRAM"));
            break;

        case "cpu":
            bars.set("load", CreatePerformanceBar("load", container, "CPU"));
            bars.set("mem", CreatePerformanceBar("ram", container, "RAM"));
            break;
    }

    if (container.childElementCount <= 0)
        container.remove();

    if (halfbarContainer.childElementCount <= 0)
        halfbarContainer.remove();

    return new PerformanceSection(background, bars, halfBars);
}

function CreatePerformanceMonitorElements() {
    PerformanceMonitor.Background = Ui.CreateElement(uiContainer, Ui.HtmlType.div, ['performance-background']);

    PerformanceMonitor.ContainerWrapper = Ui.CreateElement(PerformanceMonitor.Background, Ui.HtmlType.div, ['performance-container-wrapper']);
    PerformanceMonitor.Container = Ui.CreateElement(PerformanceMonitor.ContainerWrapper, Ui.HtmlType.div, ['performance-container']);
    PerformanceMonitor.HorizontalContainer = Ui.CreateElement(PerformanceMonitor.ContainerWrapper, Ui.HtmlType.div, ['performance-container-horizontal']);

    PerformanceMonitor.FpsSection = CreatePerformanceMonitorSection("framedata", PerformanceMonitor.Container);
    PerformanceMonitor.GpuSection = CreatePerformanceMonitorSection("gpu", PerformanceMonitor.HorizontalContainer);
    PerformanceMonitor.CpuSection = CreatePerformanceMonitorSection("cpu", PerformanceMonitor.HorizontalContainer);

    PerformanceMonitor.Background.style.opacity = 0;
    ShowPerformanceMonitor = false;
}

function HandleMessages(msg, bar) {
    var decoded = Api.Parse(msg);

    switch (decoded.Command) {
        case 'UpdateSteamAvatar':
            // Wrist.ProfileImage.src = `data:image/png;base64,${decoded.RawData}`
            break;

        case 'UpdateRuntimePerformance':
            OnUpdatePerformanceInformation(decoded.JsonData);
            break;

        case 'UpdateDateTime':
            Wrist.Date.innerHTML = decoded.JsonData.Date;
            Wrist.Clock.innerHTML = decoded.JsonData.Time;
            Wrist.TimeInVR.innerHTML = decoded.JsonData.CurrentSessionLength;
            break;

        case 'UpdateDeviceInformation':
            OnUpdateDevices(decoded.JsonData);
            break;

        case 'UpdateTheme':
            Common.ApplyTheme(decoded.JsonData);
            break;

        case 'UpdateLayoutModeState':
            OnUpdateLayoutModeState(decoded.RawData);
            break;

        case 'UpdateMediaPlayer':
            OnUpdateMediaPlayer(decoded.JsonData);
            break;

        case 'UpdateSettings':
            UpdateSettings(decoded.JsonData);
            break;

        case 'TogglePerformanceStats':
            OnTogglePerformanceMonitor(decoded.JsonData);
            break;

        case 'UpdateOverlayIDs':
            OverlayHandleID = decoded.JsonData[window.location.href]
            console.log(`Current Overlay ID : ${OverlayHandleID}`);
            break;
            
        case 'UpdateLayoutInfo':
            FavoriteLayout = decoded.JsonData.favorite;
            CurrentLayout = decoded.JsonData.current;
            AvailableLayouts = decoded.JsonData.layouts;
            
            LayoutWidget.SelectorDropdown.setValue(`${parseInt(CurrentLayout) + 1}`);
            for (var i = 0; i < LayoutWidget.SelectorDropdown.dropdownItems.length; i++) {
                var layout = decoded.JsonData.layouts[i];
                var windowCount = layout.windowCount;

                LayoutWidget.SelectorDropdown.dropdownItems[i].indicator.style.color = windowCount > 0 ? 'var(--color-accent)' : 'rgba(0,0,0,0.25)';
            }
            
            UpdateLayoutToolbarButtonStates();
            
            console.log("Updated Layout Info:", decoded.JsonData);
            break;
    }
}

function UpdateLayoutToolbarButtonStates() {
    var currentLayout = AvailableLayouts[CurrentLayout];
    var currentLayoutWindowCount = currentLayout.windowCount;
    
    LayoutWidget.SaveLayoutButton.style.color = currentLayoutWindowCount > 0 ? 'rgba(0,0,0,0.25)' : 'var(--theme-contrasting)';
    LayoutWidget.SaveLayoutButton.style.pointerEvents = currentLayoutWindowCount > 0 ? 'none' : 'auto';

    LayoutWidget.LoadLayoutButton.style.color = currentLayoutWindowCount > 0 ? 'var(--theme-contrasting)' : 'rgba(0,0,0,0.25)';
    LayoutWidget.LoadLayoutButton.style.pointerEvents = currentLayoutWindowCount > 0 ? 'auto' : 'none';

    LayoutWidget.DeleteLayoutButton.style.color = currentLayoutWindowCount > 0 ? 'var(--theme-contrasting)' : 'rgba(0,0,0,0.25)';
    LayoutWidget.DeleteLayoutButton.style.pointerEvents = currentLayoutWindowCount > 0 ? 'auto' : 'none';
    
    if (CurrentLayout == FavoriteLayout && FavoriteLayout != -1) {
        LayoutWidget.FavoriteLayoutButton.style.color = 'var(--theme-accent)';
        LayoutWidget.FavoriteLayoutButton.style.pointerEvents = 'none';
        LayoutWidget.FavoriteIconText.classList.remove('bi-star');
        LayoutWidget.FavoriteIconText.classList.add('bi-star-fill');
    }
    else {
        LayoutWidget.FavoriteLayoutButton.style.color = 'var(--theme-contrasting)';
        LayoutWidget.FavoriteLayoutButton.style.pointerEvents = 'auto';
        LayoutWidget.FavoriteIconText.classList.remove('bi-star-fill');
        LayoutWidget.FavoriteIconText.classList.add('bi-star');
    }
}

function UpdateSettings(settings) {
    let didMediaThemeSettingChange = MediaThemeing != settings.MediaThemeing;
    
    AutoMediaDetection = settings.AutoMediaDetection;
    AlwaysShowDetailedInformation = settings.AlwaysShowDetailedInformation;
    ShowBatteryPercentageInsteadOfTimeEstimate = settings.DefaultShowBatteryPercentage;
    MediaThemeing = settings.MediaThemeing;
    HideWristOverlay = settings.HideWristOverlay;

    if (HideWristOverlay) {
        Wrist.Background.classList.add('hidden');
    }
    else {
        Wrist.Background.classList.remove('hidden');
    }

    OnThemeMediaPlayer(MediaThemeing && ShowMediaPlayer, didMediaThemeSettingChange);

    if (BatteryFontScale != settings.BatteryFontScale) {
        BatteryFontScale = settings.BatteryFontScale;
        if (LastDevicePollData != null) {
            OnUpdateDevices(LastDevicePollData);
        }
    }

    console.log("Updating Settings.")
}

function OnUpdateMediaPlayer(data) {
    console.log(data);
    if (data == null) {
        OnEmptyMediaPlayer();
        return;
    }

    if (!data.title && !data.album && !data.albumArt) {
        OnEmptyMediaPlayer();
        return;
    }

    CurrentMedia.Artist = data.artist;
    CurrentMedia.Track = data.title;
    CurrentMedia.Album = data.albumTitle;
    CurrentMedia.AlbumArt = data.albumArt;
    CurrentMedia.PlaybackStatus = data.playbackStatus;

    if (CurrentMedia.PlaybackStatus == 'Playing') {
        MediaPlayer.PlayPauseIcon.classList.remove('bi-play-fill');
        MediaPlayer.PlayPauseIcon.classList.add('bi-pause-fill');
    } else {
        MediaPlayer.PlayPauseIcon.classList.remove('bi-pause-fill');
        MediaPlayer.PlayPauseIcon.classList.add('bi-play-fill');
    }

    if (CurrentMedia.Album == null || CurrentMedia.Album == 'undefined')
        CurrentMedia.Album = ' ';
    else
        CurrentMedia.Album = `- ${CurrentMedia.Album}`;

    var albumArt = CurrentMedia.AlbumArt ? `data:image/png;base64,${CurrentMedia.AlbumArt}` : `data:image/png;base64,${Common.defaultAlbumArtB64}`;

    if (AutoMediaDetection) {
        OnToggleMediaPlayer(true);
    }

    Wrist.BackgroundCover.src = albumArt;
    LayoutWidget.MediaBackground.src = albumArt;
    MediaPlayer.Icon.src = albumArt;
    MediaPlayer.Track.innerHTML = `${CurrentMedia.Track}`;
    MediaPlayer.Artist.innerHTML = `${CurrentMedia.Artist} ${CurrentMedia.Album}`;

    if (firstLoad && !AutoMediaDetection) {
        OnToggleMediaPlayer(false);
        firstLoad = false;
    }
}

function OnEmptyMediaPlayer() {
    CurrentMedia.AlbumArt = `data:image/png;base64,${Common.defaultAlbumArtB64}`;
    CurrentMedia.Track = 'No Media';
    CurrentMedia.Artist = 'No Media';
    CurrentMedia.Album = '';
    CurrentMedia.PlaybackStatus = 'Paused';

    Wrist.BackgroundCover.src = CurrentMedia.AlbumArt;
    LayoutWidget.MediaBackground.src = CurrentMedia.AlbumArt;
    MediaPlayer.Icon.src = CurrentMedia.AlbumArt;
    MediaPlayer.Track.innerHTML = `${CurrentMedia.Track}`;
    MediaPlayer.Artist.innerHTML = `${CurrentMedia.Artist}`;
    OnHideMediaElements();

    if (AutoMediaDetection) {
        OnToggleMediaPlayer(false);
    }
}

function OnToggleLayoutToolbar() {
    if (ShowLayoutsToolbar) {
        LayoutWidget.Container.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) show-layouts-widget forwards';
        LayoutWidget.Container.style.pointerEvents = 'auto';
        Wrist.Container.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) move-wrist-down-layout-toolbar forwards';
    }
    else {
        LayoutWidget.Container.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) hide-layouts-widget forwards';
        LayoutWidget.Container.style.pointerEvents = 'none';
        Wrist.Container.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) move-wrist-up-layout-toolbar forwards';
    }

    CheckPerformanceBarPosition();
}

function OnToggleMediaPlayer(override) {
    if (override != null)
        ShowMediaPlayer = override;
    else
        ShowMediaPlayer = !ShowMediaPlayer;

    Api.Send('Tweak_ToggleMediaPlayer', ShowMediaPlayer, null);

    if (ShowMediaPlayer) {
        MediaPlayer.Background.style.animation = '0.5s cubic-bezier(0.34, 1.56, 0.64, 1) show-media-player forwards';
        Wrist.LowerWidgetContainer.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) show-media-player-move-widgets forwards';
        Wrist.Background.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) show-media-player-adjust-height forwards';
        Wrist.BackgroundWidgetContainer.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) show-media-player-move-clock-and-toolbar forwards';

        OnThemeMediaPlayer(MediaThemeing);
    }
    else {
        OnHideMediaElements();
    }

    CheckPerformanceBarPosition();
}

function OnThemeMediaPlayer(useMediaTheme, sendApiEvent = true) {
    if (useMediaTheme) {
        let contrastTextColor = MediaPlayer.AlbumArtBrightness >= 400 ? 'var(--theme-dark)' : 'var(--theme-contrasting)';
        
        MiniToolbar.Background.classList.add('glass-backdrop');
        MediaPlayer.Background.classList.add('glass-backdrop');
        MediaPlayer.ControlsContainer.classList.add('glass-backdrop');
        
        let hiTone = `color-mix(in srgb, ${MediaPlayer.MediaThemeBackground} 90%, white)`;
        let midTone = `color-mix(in srgb, ${MediaPlayer.MediaThemeBackground} 100%, white)`;
        let darkTone = `color-mix(in srgb, ${MediaPlayer.MediaThemeBackground} 65%, black)`;
        let accentTone = MediaPlayer.MediaThemeAccent;

        Wrist.BackgroundCover.style.opacity = 1;
        Wrist.BackgroundGradient.style.opacity = 1;
        Wrist.BackgroundGradient.style.background = `linear-gradient(145deg, ${MediaPlayer.MediaThemeBackground} 20%, transparent 100%)`;

        Wrist.BatteryWidget.style.backgroundColor = MediaPlayer.MediaThemeBackground;
        toolbar.style.backgroundColor = MediaPlayer.MediaThemeBackground;

        Wrist.ClockContainer.style.color = contrastTextColor;
        MediaPlayer.Artist.style.color = contrastTextColor;
        MediaPlayer.Track.style.color = contrastTextColor;

        LayoutWidget.Container.style.backgroundColor = MediaPlayer.MediaThemeBackground;
        LayoutWidget.MediaBackground.style.opacity = 1;
        LayoutWidget.ButtonContainer.classList.add('glass-backdrop');
        LayoutWidget.SelectorDropdown.button.classList.add('glass-backdrop');
        LayoutWidget.SelectorDropdown.setTheme(
            MediaPlayer.MediaThemeAccent,
            hiTone,
            midTone,
            darkTone,
            contrastTextColor
        );

        if (sendApiEvent) {
            let packedMediaTheme = {
                ThemeType: Api.ThemeType.MediaTheme,
                UseMediaTheme: true,
                Hi: resolveColorMixHex(hiTone),
                Mid: resolveColorMixHex(midTone),
                Dark: resolveColorMixHex(darkTone),
                Accent: resolveColorMixHex(accentTone),
                Contrast: resolveColorMixHex(contrastTextColor)
            };
            console.log("Packed Media Theme: ", packedMediaTheme);
            Api.Send(Api.Commands.RequestSetTheme, JSON.stringify(packedMediaTheme));
            Api.Client.Root.style.setProperty('--theme-accent', MediaPlayer.MediaThemeAccent);
        }
    }
    else {
        ResetMediaThemedElements(sendApiEvent);
    }
}

// Resolve a CSS color expression (e.g., color-mix()) to #RRGGBB or #RRGGBBAA
export function resolveColorMixHex(expr) {
    const el = document.createElement("div");
    // avoid layout jank
    el.style.position = "absolute";
    el.style.left = "-99999px";
    el.style.color = expr;
    document.body.appendChild(el);

    const computed = (getComputedStyle(el).color || "").trim();
    el.remove();

    return cssComputedColorToHex(computed);
}

function cssComputedColorToHex(c) {
    if (!c) return null;

    // 1) CSS Color 4: color(srgb r g b / a?)
    let m = c.match(/^color\(srgb\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)(?:\s*\/\s*([0-9.]+))?\)$/i);
    if (m) {
        const r = Math.round(clamp01(parseFloat(m[1])) * 255);
        const g = Math.round(clamp01(parseFloat(m[2])) * 255);
        const b = Math.round(clamp01(parseFloat(m[3])) * 255);
        const a = m[4] === undefined ? 1 : clamp01(parseFloat(m[4]));
        return rgbaToHex(r, g, b, a);
    }

    // 2) rgb()/rgba(), commas OR spaces, optional "/ alpha" or ", alpha"
    //    supports percentages for channels per spec
    m = c.match(/^rgba?\(\s*([0-9.]+%?)\s*[, ]\s*([0-9.]+%?)\s*[, ]\s*([0-9.]+%?)(?:\s*[/,]\s*([0-9.]+))?\s*\)$/i);
    if (m) {
        const ch = (x) => x.endsWith("%") ? Math.round(clamp01(parseFloat(x) / 100) * 255)
            : Math.round(clamp8(parseFloat(x)));
        const r = ch(m[1]), g = ch(m[2]), b = ch(m[3]);
        const a = m[4] === undefined ? 1 : clamp01(parseFloat(m[4]));
        return rgbaToHex(r, g, b, a);
    }

    // 3) Already hex; normalize short to long
    m = c.match(/^#([0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$/i);
    if (m) {
        const h = m[1];
        if (h.length === 3 || h.length === 4) {
            const full = h.split("").map(ch => ch + ch).join("");
            return "#" + full.toLowerCase();
        }
        return c.toLowerCase();
    }

    // Unknown format; return as-is to avoid throwing
    return c;
}

function rgbaToHex(r, g, b, a = 1) {
    r = clamp8(r); g = clamp8(g); b = clamp8(b);
    const hex = "#" + [r, g, b].map(n => n.toString(16).padStart(2, "0")).join("");
    if (a >= 1) return hex;
    const ah = Math.round(a * 255).toString(16).padStart(2, "0");
    return hex + ah;
}

function clamp8(x)  { return Math.max(0, Math.min(255, Math.round(x))); }
function clamp01(x) { return Math.max(0, Math.min(1, x)); }

function OnHideMediaElements() {
    MediaPlayer.Background.style.animation = '0.5s cubic-bezier(0.34, 1.56, 0.64, 1) hide-media-player forwards';

    Wrist.LowerWidgetContainer.style.animation = '0.4s cubic-bezier(0.34, 1.56, 0.64, 1) hide-media-player-move-widgets forwards';
    Wrist.Background.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) hide-media-player-adjust-height forwards';
    Wrist.BackgroundWidgetContainer.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) hide-media-player-move-clock-and-toolbar forwards';
    ResetMediaThemedElements();
}

function ResetMediaThemedElements(sendApiEvent = true) {
    Wrist.BackgroundCover.style.opacity = 0;
    Wrist.BackgroundGradient.style.opacity = 0;
    Wrist.ClockContainer.style.color = 'var(--theme-contrasting)';
    Wrist.BatteryWidget.style.backgroundColor = 'var(--theme-mid)';
    
    LayoutWidget.Container.style.backgroundColor = 'var(--theme-dark)';
    LayoutWidget.MediaBackground.style.opacity = 0;
    LayoutWidget.SelectorDropdown.button.classList.remove('glass-backdrop');
    LayoutWidget.ButtonContainer.classList.remove('glass-backdrop');
    LayoutWidget.SelectorDropdown.resetTheme();
    
    // MediaPlayer.ControlsContainer.style.backgroundColor = 'var(--theme-accent)';
    MediaPlayer.ControlsContainer.classList.remove('glass-backdrop');
    MediaPlayer.Background.classList.remove('glass-backdrop');
    MiniToolbar.Background.classList.remove('glass-backdrop');
    toolbar.style.backgroundColor = 'var(--theme-mid)';

    if (sendApiEvent) {
        let packedMediaTheme = {
            ThemeType: Api.ThemeType.None,
            UseMediaTheme: false,
        };
        console.log("Packed Media Theme: ", packedMediaTheme);
        Api.Send(Api.Commands.RequestSetTheme, JSON.stringify(packedMediaTheme));
        Api.Client.Root.style.setProperty('--theme-accent', Api.Client.Theme.Accent);
    }
}

function OnUpdateDevices(data) {
    LastDevicePollData = data;
    if (DeviceTrackers.length != data.length) {
        let oldDeviceTrackers = document.getElementsByName('battery-bubble-container');
        for (let i = oldDeviceTrackers.length - 1; i >= 0; i--) {
            oldDeviceTrackers[i].parentNode.removeChild(oldDeviceTrackers[i]);
        }
        DeviceTrackers = [];

        data.forEach(device => {
            let parent = Ui.CreateElement(Wrist.BatteryScrollRect, Ui.HtmlType.div, ['data-bubble', 'theme-mid']);
            parent.setAttribute('name', 'battery-bubble-container');
            
            if(device.classification == Api.DeviceClass.HMD && device.waitingForFirstRealBatteryUpdate)
                return;

            CreateDeviceTracker(`device_${device.name}`, parent, device.classification);
        });
    }

    for (let i = 0; i < DeviceTrackers.length; i++) {
        UpdateDeviceTracker(DeviceTrackers[i], data[i]);
    }

    Wrist.BatteryWidget.style.width = `${Math.min(5, DeviceTrackers.length) * 70}px`;
}

function CreateDeviceTracker(id, parent, deviceClassification) {
    let container = Ui.CreateElement(parent, Ui.HtmlType.div, ['battery-data-wrapper']);
    container.id = 'battery-tracker-container';

    let icon;
    if(deviceClassification == Api.DeviceClass.HMD) {
        icon = Ui.CreateElement(container, Ui.HtmlType.img, ['theme-font-contrast', 'battery-data-icon', 'position-absolute', 'noselect']);
        icon.classList.add(`bi-headset-vr`);
    }
    else {
        icon = Ui.CreateElement(container, Ui.HtmlType.img, ['xsoverlay-icon-font', 'theme-font-contrast', 'battery-data-icon', 'position-absolute', 'noselect']);
    }
    icon.style.fontSize = `32px`;
    icon.style.animation = 'lower-battery-icon 200ms forwards'

    let batteryBackground = Ui.CreateElement(container, Ui.HtmlType.div, ['device-battery-bar-background', 'position-absolute', 'noselect'], new Ui.uiTransform(46, 2, 1));
    batteryBackground.style.animation = 'lower-battery-bar 200ms forwards'

    let battery = Ui.CreateElement(batteryBackground, Ui.HtmlType.div, ['device-battery-bar-foreground', 'position-absolute', 'noselect'], new Ui.uiTransform(46, 2, 1));

    let chargingIndicator = Ui.CreateElement(container, Ui.HtmlType.div, ['position-absolute', 'noselect']);
    chargingIndicator.classList.add(`bi-lightning-charge-fill`);
    chargingIndicator.style.fontSize = `14px`;
    chargingIndicator.style.transform = 'translate(-20px, -20px)';

    let timeEst = Ui.CreateElement(container, Ui.HtmlType.div, ['theme-font-contrast', 'position-absolute', 'time-left-text', 'noselect']);
    timeEst.style.transform = 'translate(0px, 22px)';

    var tracker = new DeviceTracker(container, `${id}`, chargingIndicator, icon, battery, batteryBackground, timeEst);
    DeviceTrackers.push(tracker);
}

function UpdateDeviceTracker(deviceTracker, device) {
    if (deviceTracker == null)
        return;

    console.log(`Updating Tracker : ${deviceTracker.classification} | ${deviceTracker.name} | Battery: ${device.battery} | Charging: ${device.charging} | Connected: ${device.connection} | Remaining: ${device.timeEstimate}`);
    deviceTracker.icon.setAttribute('mIcon', `${device.label}`);

    let percent = device.battery / 100;
    let ppx = deviceTracker.batteryBackground.offsetWidth * percent;

    deviceTracker.battery.style.width = `${ppx}px`;
    deviceTracker.timeEstimate.innerHTML = ShowBatteryPercentageInsteadOfTimeEstimate ? `${Math.round(device.battery)}%` : `${device.timeEstimate}`;
    deviceTracker.timeEstimate.style.fontSize = `${BatteryFontScale}px`;

    SetDeviceChargeState(deviceTracker, device.charging);
    SetDeviceDetailedViewState(deviceTracker, AlwaysShowDetailedInformation || ShowingDetailedBatteryInformation);

    if (device.connection != OpenVR.DeviceActivityLevel.Unknown && device.connection != OpenVR.DeviceActivityLevel.Standby) {
        if (device.battery <= 10 && device.battery > 0 && !device.charging) {
            SetDeviceTrackerLowBatteryState(deviceTracker);
        }
        else if (device.battery == 0) {
            SetDeviceTrackerInactiveState(deviceTracker);
            deviceTracker.timesPingedAtZero += 1;
        }
        else {
            SetDeviceTrackerNormalState(deviceTracker);
        }
    }
    else {
        SetDeviceTrackerInactiveState(deviceTracker);
    }
}

function SetDeviceDetailedViewState(deviceTracker, show) {
    if (show) {
        deviceTracker.timeEstimate.style.animation = 'show-time-left 200ms forwards';
        deviceTracker.batteryBackground.style.animation = 'raise-battery-bar 200ms forwards'
        deviceTracker.icon.style.animation = 'raise-battery-icon 200ms forwards'
    }
    else {
        deviceTracker.timeEstimate.style.animation = 'hide-time-left 200ms forwards';
        deviceTracker.batteryBackground.style.animation = 'lower-battery-bar 200ms forwards'
        deviceTracker.icon.style.animation = 'lower-battery-icon 200ms forwards'
    }
}

function SetDeviceTrackerInactiveState(deviceTracker) {
    deviceTracker.icon.style.color = `rgba(0, 0, 0, 0.25)`;
    deviceTracker.icon.style.transform = 'translate(0px, 0px)';
    deviceTracker.chargingIndicator.style.color = 'transparent';
    deviceTracker.battery.style.backgroundColor = `rgba(0, 0, 0, 0.25)`;
    deviceTracker.batteryBackground.style.backgroundColor = `rgba(0, 0, 0, 0.25)`;
    deviceTracker.timeEstimate.style.color = `rgba(0, 0, 0, 0.25)`;
}

function SetDeviceTrackerLowBatteryState(deviceTracker) {
    deviceTracker.chargingIndicator.style.animation = 'blink-loop 1000ms infinite';
    deviceTracker.chargingIndicator.style.color = `var(--theme-error)`;
    deviceTracker.icon.style.color = `var(--theme-accent)`;
    deviceTracker.icon.style.transform = 'translate(0px, -8px)';
    deviceTracker.battery.style.backgroundColor = `var(--theme-accent)`;
    deviceTracker.batteryBackground.style.backgroundColor = `rgba(0, 0, 0, 0.65)`;
    deviceTracker.timeEstimate.style.color = `var(--theme-contrasting)`;
}

function SetDeviceTrackerNormalState(deviceTracker) {
    deviceTracker.icon.style.color = `var(--theme-accent)`;
    deviceTracker.icon.style.transform = 'translate(0px, -8px)';
    deviceTracker.battery.style.backgroundColor = `var(--theme-accent)`;
    deviceTracker.batteryBackground.style.backgroundColor = `rgba(0, 0, 0, 0.65)`;
    deviceTracker.timeEstimate.style.color = `var(--theme-contrasting)`;
}

function SetDeviceChargeState(deviceTracker, isCharging) {
    deviceTracker.chargingIndicator.style.animation = '';
    deviceTracker.chargingIndicator.style.color = isCharging ? 'var(--theme-accent)' : 'rgba(0, 0, 0, 0.25)';
}

function rgb(values) {
    return 'rgb(' + values.join(', ') + ')';
}

function OnUpdateLayoutModeState(data) {
    let index = 0;
    let updateDelayTotal = 0;

    if (data == 'True') {
        IsLayoutMode = true;
        ShowLayoutsToolbar = WasShowingLayoutsToolbar;
        ToolbarButtons.forEach(element => {
            index++;
            let delay = 0.025 * index * 1000;
            updateDelayTotal += delay;

            setTimeout(() => {
                element.style.animation = '0.35s cubic-bezier(0.34, 1.56, 0.64, 1) showToolbarButton';
            }, delay);

            setTimeout(() => {
                element.style = '';
            }, delay + (0.35 * 1000));
        });

        toolbar.style.animation = '0.2s linear fade-in-opacity forwards';

        Api.Send(Api.Commands.RequestLayoutInfo);
    }
    else {
        WasShowingLayoutsToolbar = ShowLayoutsToolbar;
        
        IsLayoutMode = false;
        ShowLayoutsToolbar = false;
        ToolbarButtons.forEach(element => {
            index++;

            let delay = 0.025 * index * 1000;
            updateDelayTotal += delay;

            setTimeout(() => {
                element.style.animation = '0.5s cubic-bezier(0.68, -0.6, 0.32, 1.6) hideToolbarButton';
                element.style.animationFillMode = 'forwards';
            }, delay);
        });

        setTimeout(() => {
            toolbar.style.animation = '0.2s linear fade-out-opacity forwards';
        }, 200);
    }

    setTimeout(() => {
        Api.Send(Api.Commands.RequestUpdateCursorCollisionTexture, null, OverlayHandleID);
    }, updateDelayTotal);

    CheckUIResolution();
    OnToggleLayoutToolbar();
    CheckPerformanceBarPosition();
}

function CheckPerformanceBarPosition() {
    let isLayoutClosedIsMediaOpen = !IsLayoutMode && ShowMediaPlayer;
    let isLayoutClosedIsMediaClosed = !IsLayoutMode && !ShowMediaPlayer;
    let isLayoutOpenIsMediaOpen = IsLayoutMode && ShowMediaPlayer;
    let isLayoutOpenIsMediaClosed = IsLayoutMode && !ShowMediaPlayer;
    let isLayoutWidgetOpen = ShowLayoutsToolbar;

    let desiredOffset = 0;
    if (IsLayoutMode) {
        if (!isLayoutWidgetOpen) {
            desiredOffset = 5;
        }
        else {
            desiredOffset = ShowMediaPlayer ? 10 : 5;
        }
    } else {
        if (!isLayoutWidgetOpen) {
            desiredOffset = ShowMediaPlayer ? -25 : -65;
        }
        else {
            desiredOffset = ShowMediaPlayer ? 10 : -20;
        }
    }
    
    PerformanceMonitor.Background.style.transform = `translateY(${desiredOffset}px)`;
}

function OffsetArrayRight(arr) {
    if (arr.length === 0) {
        return arr;
    }
    const lastElement = arr.pop(); // Remove the last element
    arr.unshift(lastElement); // Add it to the front of the array
    return arr;
}

function GetTextForBarValue(name, value) {
    let text = '';
    switch (name) {
        case 'load':
            text = `${Math.round(value)} <small>%</small>`;
            break;

        case 'mem':
            let gigabytes = value /= 1000;
            text = `${gigabytes.toFixed(1).replace(/\.00$/, '')} <small>GB</small>`
            break;

        case 'framerate':
            text = `${Math.round(value)}`;
            break;

        case 'gpuFrametime':
            text = `${value.toFixed(1).replace(/\.00$/, '')} <small>ms</small>`
            break;

        case 'cpuFrametime':
            text = `${value.toFixed(1).replace(/\.00$/, '')} <small>ms</small>`
            break;
    }
    return text;
}

function UpdatePerformanceMonitorSection(section, sectionLoads, sectionMaxLoads, sectionInverse, extraInfo) {
    let index = 0;
    for (let [key, value] of section.bars) {
        let barDisplay = value;
        let barName = key;

        let currentValue = sectionLoads[index];
        let maxValue = sectionMaxLoads[index];

        let percent = Math.min(1, currentValue / maxValue);
        let percentWidth = barDisplay.barBackground.offsetWidth * percent;

        barDisplay.bar.style.width = `${percentWidth}px`;
        barDisplay.percent.innerHTML = GetTextForBarValue(barName, currentValue);

        if (extraInfo != null) {
            let currentExtra = extraInfo[index];
            barDisplay.extra.innerHTML = currentExtra == null ? '' : currentExtra;
        }
        else {
            barDisplay.extra.innerHTML = '';
        }
        index++;
    }
}

function OnUpdatePerformanceInformation(data) {
    let system = new Api.SystemPerformanceObject(data);
    console.log(system);

    var totalFrametime = system.TotalFrametime.toFixed(1).replace(/\.00$/, '');
    var maxFrametime = system.MaxFrametime.toFixed(1).replace(/\.00$/, '');
    UpdatePerformanceMonitorSection(
        PerformanceMonitor.FpsSection,
        [system.Framerate, system.GpuFrametime, system.CpuFrametime],
        [system.MaxFramerate, system.MaxFrametime, system.MaxFrametime],
        [false, false, false],
        [`${totalFrametime} <small>ms</small> (${maxFrametime} <small>ms</small>)`, null, null]
    );

    UpdatePerformanceMonitorSection(
        PerformanceMonitor.GpuSection,
        [system.GPU.Load, system.GPU.MemLoad],
        [100, system.GPU.MemSize],
        [false, false, false],
        [`${Math.round(system.GPU.Temperature)}°`, null]
    );

    var cpuTemp = Math.round(system.CPU.Temperature) == 0 ? '' : `${Math.round(system.CPU.Temperature)}°`;
    UpdatePerformanceMonitorSection(
        PerformanceMonitor.CpuSection,
        [system.CPU.Load, system.Memory.MemLoad],
        [100, system.Memory.MemSize],
        [false, false, false],
        [cpuTemp, null]
    );
}

function CheckUIResolution() {
    CurrentResolution.width = 740;
    CurrentResolution.height = 450;

    Api.Send(Api.Commands.RequestUpdateOverlayCanvasSize, JSON.stringify(CurrentResolution), OverlayHandleID);
}

function OnTogglePerformanceMonitor(jsonData) {
    ShowPerformanceMonitor = !ShowPerformanceMonitor;

    if (ShowPerformanceMonitor) {
        PerformanceMonitor.Background.style.opacity = 1;
        Wrist.Container.style.left = `${0}px`;
        PerformanceMonitor.Background.style.left = `505px`;
        PerformanceMonitor.Background.style.pointerEvents = 'auto';
        Api.Send(Api.Commands.SubscribeToEvents, JSON.stringify([Api.SubscriptionTag.Performance]));
    }
    else {
        PerformanceMonitor.Background.style.opacity = 0;
        Wrist.Container.style.left = `${WristHorizontalOffset}px`;
        PerformanceMonitor.Background.style.left = `450px`;
        PerformanceMonitor.Background.style.pointerEvents = 'none';
        Api.Send(Api.Commands.UnsubscribeToEvents, JSON.stringify([Api.SubscriptionTag.Performance]));
    }

    CheckUIResolution();
}

function CalculateStrokeArray(element) {
    var d = element.getBoundingClientRect().width;
    var r = d / 2;
    var c = Math.PI * r * 2;
    return c;
}

