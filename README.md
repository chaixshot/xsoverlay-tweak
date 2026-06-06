<div align="center">

  # XSOverlay Tweak
  ### A comprehensive enhancement mod for  [XSOverlay](https://store.steampowered.com/app/1173510/XSOverlay/) that provides advanced performance tuning, cursor improvements, custom interaction mechanics, and various quality-of-life fixes.
</div>

## 🖥️ Screenshot
<img src="./img/screenshot_1.jpeg" width="800"> <img src="./img/screenshot_2.jpeg" width="400"> <img src="./img/screenshot_3.jpeg" width="400">

## Features

### 🚀 Refresh Rate
- **Target Refresh Rate**: Set a custom frame rate for XSOverlay rendering to improve responsiveness or reduce CPU usage.
- **Contextual Performance**: Optionally apply high refresh rates only when hovering over an overlay or while in Layout Mode.

### 🖱️ Cursor & Mouse
- **Windows Cursor Pointer**: Replaces the standard pointer with the actual Windows cursor image for a native "SteamVR Dashboard" feel.
- **Latency Reduction**: "Always Update" sends cursor positions before desktop frames are captured to eliminate one-frame lag.
- **Mouse Smoothing**: Multiple levels of smoothing (Ultra Low to Very High) for high-precision desktop interaction.
- **Physical Mouse Detector:** Relinquishes VR pointer control automatically when physical mouse movement is detected.
- **Always Hide**: Forcefully hides the system cursor in capture overlays.

### 👈 Pointer & Interaction
- **Active Click**: Clicking with the inactive hand makes it active and performs a click simultaneously for seamless two-hand interaction.
- **Inactive Pointer Visuals**: Highlights the inactive hand's pointer in red with adjustable opacity for easier identification.
- **WebView Integration**: Applies inactive pointer features to internal UIs like Settings, Wrist, and other WebViews.
- **Scale Multiplier**: Adjust the pointer scale relative to global XSOverlay settings.
- **Double Click Delay**: Applies XSOverlay's double-click delay to the physical pointer itself.
- **Click Animation**: Enables visual feedback for clicks when using Emulate Mouse mode.

### 🎮 Mouse Navigation
- **Browser Navigation**: Custom SteamVR bindings for Mouse Forward and Mouse Back actions.
- **Keyboard Emulation**: Option to use `Alt + Left/Right` shortcuts for navigation, targeting the focused window.

### 🖥️ Dashboard Overlay
- **Persistent Visibility**: Choose which overlays remain visible and interactive while the SteamVR Dashboard is open:
  - Notifications, Pointer, Settings, Capture Windows, Wrist, and Keyboard.

### 📳 Haptic Feedback
- **Granular Feedback**: Individual strength sliders (0-100%) for various interactions:
  - Grabbing, Keyboard Hover/Press, Overlay Swapping, WebView interaction, and Pointer Locking.
- **Contextual Vibrations**: Haptic feedback for Sticky Keys and toggling Layout Mode.

### ⚡ Optimization
- **Efficiency Mode**: Enables Windows Efficiency Mode for the XSOverlay process to minimize resource usage.
- **Inactive Refresh Rate**: Set a very low target frame rate (down to 5 FPS) when not interacting with any overlays.

### ✨ Quality of Life
- **Laser Pointer**: Renders a laser from controllers (mimicking SteamVR) for more accurate targeting.
- **Trigger Lock**: "Pull Trigger Pointer Lock" keeps the pointer steady while clicking to prevent accidental movement.
- **Interaction Logic**: Enhances "Pinned" or "Block Input" overlays to only allow interaction when Layout Mode is active.
- **WebView Enhancements**: Wider scrollbars for easier VR interaction.
- **Wrist Improvements**: Increased allowed positioning radius for the Wrist Overlay.
- **Visual Fixes**: Initializes capture overlays with a white texture to prevent them from appearing invisible on spawn.

### 🔧 Fixes
- **Sticky Ctrl Fix**: Resolves the issue where the Ctrl key fails to remain "sticky" correctly.
- **Layout Scale Fix**: Ensures saved scale values are applied accurately when loading overlay layouts.
- **Rendering Fixes**: Prevents overlays from turning invisible during simultaneous rotation and curvature changes.
- **WebView Click Fix**: Fixes unclickable UI elements in certain internal WebView displays.

## ⛏️ Installation
1. Download the plugin ZIP from [Releases](https://github.com/chaixshot/xxsoverlay-tweak/releases/latest)
2. Extract the ZIP and drop the files and folders inside ``xsoverlay-tweak`` to ``[Steam]/steamapps/common/[XSOverlay]``
3. Launch XSOverlay.
4. Enjoy!

## ⚙️ Configuration

This mod injects a custom settings page directly into the XSOverlay UI.

1. Open the XSOverlay **Settings** menu.
2. Click on the **XSOverlay Tweak** (wrench icon) tab in the sidebar.
3. Adjust settings in real-time.

## 🖱️ Mouse Navigation Setup
To use the Mouse Back/Forward features:
1. Open XSOverlay Settings and go to the **Bindings** tab.
2. This opens the SteamVR bindings menu.
3. Edit your current binding and add a button for the `MouseBack` or `MouseForward` actions.

## ⛔ Disable
Go to ``[Steam]/steamapps/common/[XSOverlay]/BepInEx/plugins/`` and remove ``xsoverlay_tweak.dll``

## 🗑️ Uninstall
Go to ``[Steam]/steamapps/common/[XSOverlay]`` and remove ``BepInEx``, ``doorstop_config.ini``, ``winhttp.dll``

## Credits
- **[XSOverlay](https://store.steampowered.com/app/1173510/XSOverlay/):** The original application by XiS.
- **[BepInEx](https://github.com/bepinex/bepinex):** For the plugin framework.
