<div align="center">

  # XSOverlay Tweak
  ### Quality-of-life [XSOverlay](https://store.steampowered.com/app/1173510/XSOverlay/) improvements, including frame rate override, pointer laser, and issue fixes. 
</div>

## 🖥️ Screenshot
<img src="./img/screenshot_1.jpeg" width="800"> <img src="./img/screenshot_2.jpeg" width="400"> <img src="./img/screenshot_3.jpeg" width="400">

## Features

### 🚀 Refresh Rate
- **Refresh Rate**: The target frame rate for XSOverlay rendering. Higher FPS reduce latency but increase CPU usage.
- **Only Hover Overlay**: Apply the custom Refresh Rate only when a Pointer is hovering over an Overlay.
- **Only In Layout Mode**: Apply the custom Refresh Rate only when Layout Mode is active.

### 🖱️ Cursor
- **Always Hide**: Forcefully hides the system Windows Cursor in Desktop and Window Capture Overlay.
- **Always Update**: Reduces Windows Cursor latency by sending the position from the Pointer before the desktop frame is captured. Without this, the Windows Cursor often appears to lag one frame behind the Pointer position.
- **Cursor Moving Interaction**: Fix where Windows cursor movement events fail to interact with elements. For example, hovering the cursor over the Windows taskbar displays a thumbnail preview, or dragging to move the system tray icon.
- **Double Click Confirm**: Ensures that a Double Click is reliable and precise, using Double Click Delay from XSOverlay settings and Windows Double-click speed setting.
- **Handle Scrolling**: Support horizontal scrolling and control scroll speed with the thumbstick axis value.
- **Mouse Smoothing**: Adjusts the level of smoothing applied to the Windows Cursor within Capture Overlay.
- **Windows Cursor Pointer**: Hides the Capture Overlay Cursor and uses the Windows Cursor image as the Pointer to mimic the SteamVR Dashboard.
  - **Animated**: always updates the cursor texture, which might impact performance.
- **Pull Trigger Click Threshold**: The Trigger pull threshold required to trigger a Left Click.
  - Uses the Trigger Value from SteamVR Input.

### 👈 Pointer
- **Active WebViews**: Applies the inactive Pointer features to WebView Overlay such as Settings, Wrist, and others.
- **Emulate Mouse Click Animation**: Enables the Pointer click visual animation for Input Method > Emulate Mouse.
- **Inactive Highlight**: Highlights the inactive hand's Pointer in red for easier identification.
- **Inactive Opacity**: Sets the opacity level for the inactive hand's Pointer.
- **Double Click Delay**: Applies the Double Click Delay from XSOverlay settings to the physical Pointer itself, not just the cursor.
- **Scale Multiplier**: Multiplier for the Pointer scale relative to the global XSOverlay setting.
- **Pull Trigger Pointer Lock/Smooth**: Locks/Smooths the Pointer while the Trigger is held for easier double clicking.
  - Uses the Trigger Value from SteamVR Input and Double Click Delay from XSOverlay settings.
- **Two Handed Mode**: Allow both hands to become active hands at the same time to perform a Click simultaneously for two-hand interaction.

### 🖐️ Wrist
- **fpsVR Socket**: Attaches the fpsVR overlay to a specific socket position of XSOverlay.
- **Hide Battery**: Hide the Wrist Overlay battery information widget.
- **Hide Invalid Battery**: Hide the invalid battery device from Wrist Overlay.
- **Wrist Clip Distance**: Wrist Overlay auto hide based on head distance.
- **Wrist Over Position**: Increases the allowed positioning radius of the Wrist Overlay.
- **Wrist State Restore**: Restore the last Wrist Overlay state at launch.

### ⌨️ Keyboard
- **Ctrl Key Sticky**: Added double-tap to the Ctrl key for sticky toggle.
- **Keyboard Control Button State**: Fix keyboard control button color not following the state when summoning.
- **Keyboard Holding Indicator**: Do Keyboard key-pressed animation while the key is being held or sticky.
- **Layout Keyboard State**: Layout will save the current keyboard state to the selected profile.

### 🖱️ Mouse
- **Mouse Button Swap**: Detect the Windows setting 'Switch primary and secondary buttons' to auto-swap controller binding.
- **Mouse Navigation**: Custom keybindings for Mouse Forward/Back navigation.
  - Press 'Bindings' tab in XSOverlay settings to open   SteamVR bindings menu.   Edit the Current Binding, add and assign the button click mode   for 'MouseBack' and 'Forward'.
  - **Mouse 4/Mouse**: target at the hovering window.
  - **Alt + Left/Right**: target at the focused window.
- **Physical Mouse Detector**: Relinquishes Pointer control when physical mouse movement is detected. Pointer Click to regain control.

### 🪟 Focused Window
- **Elevated**: Do action when the focused window is running as Administrator and XSOverlay is running as User to prevent interaction deadlock.
- **Hang**: Do action when the focused window is hung or not responding to prevent interaction deadlock.
- **Fullscreen**: Do action if the current focused or game window is in fullscreen mode when toggling on Layout Mode.

### �️ Dashboard Overlay
- **Dashboard Notification**: Allows Notifications to be displayed over the SteamVR Dashboard.
- **Dashboard Pointer**: Allows the Pointer to be displayed and interactive over the SteamVR Dashboard.
- **Dashboard Settings**: Allows the Settings WebView Overlay to be displayed over the SteamVR Dashboard.
- **Dashboard Window**: Allows Capture Overlay to be displayed over the SteamVR Dashboard.
- **Dashboard Wrist**: Allows the Wrist Overlay to be displayed over the SteamVR Dashboard.
- **Dashboard Keyboard**: Allows the Keyboard to be displayed over the SteamVR Dashboard.

### 📳 Haptic Feedback
- **Double Click**: Plays a haptic feedback when Double Click.
- **Grab**: Plays a haptic feedback when grabbing any Overlay.
- **Keyboard Key**: Plays a haptic feedback when Pointer is hovering a Keyboard key.
- **Keyboard Press**: Plays a haptic feedback when Pointer is pressing a Keyboard key.
- **Overlay Swapping**: Plays a haptic feedback when Pointer is switching Overlay.
- **Sticky Key Haptic**: Plays a haptic feedback when a sticky key is pressed.
- **Pull Trigger Pointer Lock Haptic**: Plays a haptic feedback when Pull Trigger Pointer Lock.
- **Toggle Layout Mode**: Plays a haptic feedback when toggle Layout Mode.
- **WebView**: Plays a haptic feedback when Pointer is hovering a WebView element.

### ⚡ Optimization
- **Efficiency Mode**: Enables Windows Efficiency Mode for XSOverlay to reduce CPU usage when not interacting with any Overlay.
  - **Pinned Visible**: does not trigger when Pinned Overlay is still visible in the play space.
- **Inactive Refresh Rate**: The target Refresh Rate for XSOverlay rendering when not interacting with any Overlay. Very low value: the Layout Mode Toggle binding listener will miss some frames.
- **OSC Thread Loop**: Instead of connecting to OSC in the loop thread, connect to the OSC server when new data is sent.

- **✨ Overlay**
- **Default Capture Overlay Texture**: Initializes a Capture Overlay with a white texture to prevent new spawns from appearing invisible.
- **Overlay Attach Smooth**: When Capture Overlay is attached to the device, it will add more options to the Window Settings flyout to control Overlay movement behavior, using Position Dampening and Rotation Dampening settings to smooth its movement.
- **Overlay Confirm Close**: Requires pressing the close overlay button three times to close.
- **Overlay Curve Auto Refresh**: Automatically applies Overlay Curve changes to all active behaviors. For example, when the Overlay Curve setting changes, Overlay Scaling and Overlay Spawning are affected.
- **Overlay Grip Anti Slip**: Prevents Overlay from dropping or slipping out of Grip when moving it too fast.
- **Overlay Roll Curve**: Prevents an Overlay from turning invisible when curvature and rotation change simultaneously.
- **Pin + Block Input Non Layout Mode**: Blocks interaction with 'Pinned' + 'Block Input' Overlay unless Layout Mode is active.
- **Window Toolbar Gesture**: When hovering over the Window Toolbar, right-click to switch to the previous Window or use thumbstick scrolling the Window list.
- **Window Toolbar Keyboard**: Add a keyboard summon button to the Capture Overlay Toolbar.

### ✨ Quality of Life
- **Laser**: Draws a Laser Pointer from the VR controllers to mimic the SteamVR Dashboard for accurate targeting.
  - **Mouse Smooth**: apply mouse smooth behavior to the Laser when active.
- **Notification Leashed Tracker**: Notification tracking using leash-like behavior instead of smooth.
- **WebView Wider Scroll**: Makes the WebView scrollbar wider for easier interaction.
- **Windows Accent Color**: Using Windows accent color as XSOverlay accent color.

### 🔧 Fixes
- **Load Layout Scale**: Ensures saved scale values are applied correctly when loading an Overlay Layout.
- **SteamVR Compositor Texture Format**: Wraps SteamVR compositor textures using the native DXGI format reported by OpenVR to avoid RGBA/BGRA shader resource view mismatches.
- **WebView**: Fixes an issue where certain WebView UI elements were not clickable.

## ⛏️ Installation
1. Download the plugin ZIP from [Releases](https://github.com/chaixshot/xsoverlay-tweak/releases/latest)
2. Extract the ZIP and drop the files and folders inside ``xsoverlay-tweak`` to ``[Steam]/steamapps/common/[XSOverlay]``
3. Launch XSOverlay.
4. Enjoy!
> The release ZIP file contains files from the [BepInEx Installation](https://github.com/BepInEx/BepInEx/wiki/Installation)

## ⚙️ Configuration

This mod injects a custom settings page directly into the XSOverlay UI.

1. Open the XSOverlay **Settings** menu.
2. Click on the **XSOverlay Tweak** (wrench icon) tab in the sidebar.
3. Adjust settings in real-time.

## 🖱️ Mouse Navigation Setup
To use the Mouse Back/Forward features:
1. Open XSOverlay Settings and go to the **Bindings** tab.
2. This opens the SteamVR bindings menu.
3. Edit your current binding and add a button for the `Mouse Back` and `Mouse Forward` actions.

## ⛔ Disable
Go to ``[Steam]/steamapps/common/[XSOverlay]/BepInEx/plugins/`` and remove ``xsoverlay_tweak.dll``

## 🗑️ Uninstall
Go to ``[Steam]/steamapps/common/[XSOverlay]`` and remove ``BepInEx``, ``doorstop_config.ini``, ``winhttp.dll``

## 🔨 Build From Source
1. Download the repo from [GitHub](https://github.com/chaixshot/xsoverlay-font-changer/archive/refs/heads/main.zip)
2. Open ***.sln** via [Visual Studio 2026](https://visualstudio.microsoft.com/downloads/)
3. Download libraries from [BepInEx Installation](https://github.com/BepInEx/BepInEx/wiki/Installation) - **BepInEx_win_x64_*.zip**
4. Change project **Dependency Assenbiles** path to `./BepInEx/core` and `./XSOverlay/XSOverlay_Data/Managed`
5. Build Solution (Ctrl+Shift+B)

## Other Mods
- [Xsoverlay Font Changer](https://github.com/chaixshot/xsoverlay-font-changer): Change the XSOverlay font to your own lovely one.
- [Xsoverlay Keyboard OSC](https://github.com/nyakowint/xsoverlay-keyboard-osc): Make XSOverlay a usable chatbox input for VRChat through OSC.

## Credits
- **[XSOverlay](https://store.steampowered.com/app/1173510/XSOverlay/):** The original application by XiS.
- **[BepInEx](https://github.com/bepinex/bepinex):** For the plugin framework.
