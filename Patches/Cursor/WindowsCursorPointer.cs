using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using uWindowCapture;
using Valve.VR;
using XSOverlay;
using xsoverlay_tweak.Patches.Pointer;
using xsoverlay_tweak.Patches.QualityOfLife;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class WindowsCursorPointer
    {
        private const int CURSOR_SHOWING = 0x00000001;
        private static readonly int CURSORINFO_SIZE = Marshal.SizeOf(typeof(CURSORINFO));
        private static readonly int BITMAP_SIZE = Marshal.SizeOf(typeof(BITMAP));

        // Reusable buffers to reduce GC allocations
        private static byte[] _rawPixelBuffer = new byte[64 * 64 * 4];

        private class CursorData
        {
            public Texture2D CursorTexture;
            public Color32[] ColorBuffer;
            public IntPtr LastCursorHandle = IntPtr.Zero;
            public Vector2 HotSpotOffset = Vector2.zero;
            public UI_RelativeTransformManipulator RelativeTransform;
            public bool IsCursor = false;
        }

        private class XSWindowResult { public bool IsMatch; }
        private static readonly ConditionalWeakTable<Raycaster, CursorData> CursorDictionary = new();
        private static readonly ConditionalWeakTable<Unity_Overlay, XSWindowResult> IsXSWindowCache = new();

        private static readonly AccessTools.FieldRef<UI_RelativeTransformManipulator, bool> ScaleByDistance_Ref = AccessTools.FieldRefAccess<UI_RelativeTransformManipulator, bool>("ScaleByDistance");
        private static readonly AccessTools.FieldRef<Raycaster, bool> CursorLocked_Ref = AccessTools.FieldRefAccess<Raycaster, bool>("CursorLocked");

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartRaycasterInstance(Raycaster __instance)
        {
            if (!IsHand(__instance)) return;

            if (IsEnable())
                CursorDictionary.Add(__instance, new());

            XConfig.WindowsCursorPointer.SettingChanged += (Event, Args) =>
            {
                if (IsEnable())
                    CursorDictionary.Add(__instance, new());
                else
                    if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                    {
                        UnityEngine.Object.Destroy(Data.CursorTexture);
                        CursorDictionary.Remove(__instance);
                    }
            };

            EventBridge.InputMethodChanged += () =>
            {
                if (IsEnable())
                    CursorDictionary.Add(__instance, new());
                else
                    if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                    {
                        UnityEngine.Object.Destroy(Data.CursorTexture);
                        CursorDictionary.Remove(__instance);
                    }
            };
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void ChangePointerTextureToWindowsCursor(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay, ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay, ref Texture2D ___CursorIcon)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
            {
                Unity_Overlay hoveringOverlay = __instance.HoveringOverlay;
                if (hoveringOverlay != null && EventBridge.IsActiveHand(__instance) && __instance.HeldOverlay == null && hoveringOverlay.IsDesktopCapture && IsTargetWindow(hoveringOverlay))
                {
                    CURSORINFO ci = new() { cbSize = CURSORINFO_SIZE };

                    if (GetCursorInfo(out ci) && (ci.flags & CURSOR_SHOWING) != 0)
                    {
                        ___VisualCursorElementOverlay.AutoUpdateOverlayTexture = false;
                        ___VisualCursorElementOverlay.colorTint = Color.white;

                        // Only update the texture if the cursor handle has actually changed
                        if (ci.hCursor != Data.LastCursorHandle || Data.CursorTexture == null || ___VisualCursorElementOverlay.overlayTexture != Data.CursorTexture)
                        {
                            Data.IsCursor = true;
                            Data.LastCursorHandle = ci.hCursor;

                            // Prevent GPU memory leak: Destroy the old texture before creating a new one
                            if (Data.CursorTexture != null)
                                UnityEngine.Object.Destroy(Data.CursorTexture);

                            // Force software overrides any custom textures the engine loaded
                            Data.CursorTexture = ExtractCurrentWindowsCursor(ci.hCursor, Data, out Data.HotSpotOffset);

                            // Disable pointer scale by distance
                            if (Data.RelativeTransform == null)
                                Data.RelativeTransform = ___VisualCursorElementOverlay.GetComponent<UI_RelativeTransformManipulator>();

                            ScaleByDistance_Ref(Data.RelativeTransform) = false;

                            float Width = hoveringOverlay.renderTexWidthOverride;
                            float Height = hoveringOverlay.renderTexHeightOverride;
                            float num3 = hoveringOverlay.widthInMeters;

                            if (Height > Width)
                                num3 *= Height / Width;

                            float widthInMeters = (0.012f * (Data.CursorTexture.width / 32f)) * num3 * PointerScaleMultiply.GetScale();
                            ___VisualCursorElementOverlay.overlayTexture = Data.CursorTexture;
                            ___VisualCursorElementOverlay.overlay.overlayTexture = Data.CursorTexture;
                            ___VisualCursorElementOverlay.widthInMeters = widthInMeters;
                            ___VisualCursorElementOverlay.overlay.overlayWidthInMeters = widthInMeters;
                        }
                    }
                    else if (Data.IsCursor)
                        ResetToDefaultCursor(__instance, ___VisualCursorElementOverlay, ___CursorIcon, Data);
                }
                else if (Data.IsCursor)
                    ResetToDefaultCursor(__instance, ___VisualCursorElementOverlay, ___CursorIcon, Data);
            }
        }

        private static bool IsTargetWindow(Unity_Overlay overlay)
        {
            if (IsXSWindowCache.TryGetValue(overlay, out XSWindowResult result))
                return result.IsMatch;

            // XSOverlay Window names are consistent, we only check this once per overlay instance
            bool isMatch = overlay.overlayName.IndexOf("XSOverlay Window", StringComparison.Ordinal) >= 0;
            IsXSWindowCache.Add(overlay, new XSWindowResult { IsMatch = isMatch });
            return isMatch;
        }

        private static void ResetToDefaultCursor(Raycaster instance, Unity_Overlay visualOverlay, Texture2D defaultIcon, CursorData data)
        {
            data.IsCursor = false;
            if (data.RelativeTransform != null)
                ScaleByDistance_Ref(data.RelativeTransform) = true;

            CursorLocked_Ref(instance) = true;
            visualOverlay.AutoUpdateOverlayTexture = true;
            visualOverlay.overlayTexture = defaultIcon;
        }

        [HarmonyPatch("SetVisualCursorTransform")]
        [HarmonyPostfix]
        public static void CursorParallelToCurveoverlay(Raycaster __instance, ref MouseInputDevice ___InputDevice, ref VROverlayIntersectionResults_t rayHitResults, ref GameObject ___VisualCursorElement)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                if (Data.IsCursor)
                {
                    PullTriggerPointerLock.InstanceState.TryGetValue(__instance, out PullTriggerPointerLock.RaycasterState DoubleClickDelayState);

                    // vNormal is the local surface normal from SteamVR.
                    Vector3 localNormal = new(rayHitResults.vNormal.v0, rayHitResults.vNormal.v1, rayHitResults.vNormal.v2);
                    Vector3 worldNormal = __instance.HoveringOverlay.transform.TransformDirection(localNormal);

                    worldNormal.x = -worldNormal.x; // Mirror X in world space to align with Unity's coordinate system for the cursor plate.

                    // Calculate the tilt required to stay parallel to the curved surface at this specific point.
                    Quaternion surfaceTilt = Quaternion.FromToRotation(Vector3.forward, worldNormal);

                    // Apply the surface tilt to the overlay's base world rotation.
                    if (!___InputDevice.ClickFreezeActive && (DoubleClickDelayState == null || !DoubleClickDelayState.IsBlock))
                        ___VisualCursorElement.transform.rotation = __instance.HoveringOverlay.transform.rotation * surfaceTilt;
                }
        }

        [HarmonyPatch("HandleClicksForDesktopWindows")]
        [HarmonyPrefix]
        public static bool SetCursorPositionBeforeClick(Raycaster __instance, ref ClickActions clickActions, ref MouseInputDevice ___InputDevice)
        {
            if (!IsEnable()) return true;
            if (!IsHand(__instance)) return true;

            if (___InputDevice.InputSource == clickActions.InputSource && __instance.CanClickDesktopCursor)
                if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                    if (Data.IsCursor)
                    {
                        RayCastResult? desktopCoordinate = EventBridge.Ref_Raycaster.GetDesktopCoordinate(__instance);
                        MouseOperations.SetCursorPosition((int)desktopCoordinate.Value.desktopCoord.x, (int)desktopCoordinate.Value.desktopCoord.y);
                    }

            return true;
        }

        public static bool IsEnable()
        {
            return XConfig.WindowsCursorPointer.Value && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;
        }

        private static bool IsHand(Raycaster __instance)
        {
            return __instance.HapticDeviceName != Raycaster.HapticDevice.None;
        }

        //?? --- Win32 API Interop ---
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        static extern int GetObject(IntPtr hObject, int nCount, out BITMAP lpObject);

        [DllImport("gdi32.dll")]
        static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, byte[] lpvBits);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        private unsafe static Texture2D ExtractCurrentWindowsCursor(IntPtr hCursor, CursorData data, out Vector2 hotSpot)
        {
            hotSpot = Vector2.zero;

            // Extract properties and bitmap references
            if (!GetIconInfo(hCursor, out ICONINFO iconInfo)) return null;

            // Keep track of the click hotspot (usually 0,0 for standard arrow tips)
            hotSpot = new Vector2(iconInfo.xHotspot, iconInfo.yHotspot);

            // Process the color bitmap structure
            if (iconInfo.hbmColor == IntPtr.Zero)
            {
                DeleteObject(iconInfo.hbmMask);
                return null;
            }

            GetObject(iconInfo.hbmColor, BITMAP_SIZE, out BITMAP bm);

            int totalBytes = bm.bmWidthBytes * bm.bmHeight;
            if (_rawPixelBuffer.Length < totalBytes) _rawPixelBuffer = new byte[totalBytes];

            GetBitmapBits(iconInfo.hbmColor, totalBytes, _rawPixelBuffer);

            // Instantiate our target Unity texture format (Windows uses BGRA)
            int w = bm.bmWidth;
            int h = bm.bmHeight;
            int hx = iconInfo.xHotspot;
            int hy = iconInfo.yHotspot;

            // Use a square canvas based on the largest required dimension to keep the hotspot centered.
            // This simplifies the physical scaling math in the Update loop.
            int size = Math.Max(Math.Max(hx, w - hx), Math.Max(hy, h - hy)) * 2;
            int W = size;
            int H = size;

            // Optimization: Reuse existing texture if dimensions match to avoid GPU re-allocation
            Texture2D texture = data.CursorTexture;
            if (texture == null || texture.width != W || texture.height != H)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
                data.ColorBuffer = new Color32[W * H];
            }

            Array.Clear(data.ColorBuffer, 0, data.ColorBuffer.Length);

            int offsetX = (W / 2) - hx;
            int offsetY = (H / 2) - hy;
            int targetYBase = H - 1 - offsetY;
            int stride = bm.bmWidthBytes;

            fixed (Color32* pColors = data.ColorBuffer)
            fixed (byte* pPixels = _rawPixelBuffer)
            {
                for (int y = 0; y < h; y++)
                {
                    byte* srcRowPtr = pPixels + (y * stride);
                    Color32* targetRowPtr = pColors + ((targetYBase - y) * W + offsetX);

                    for (int x = 0; x < w; x++)
                    {
                        // BGRA (Windows) to RGBA (Unity) conversion with pointer arithmetic
                        targetRowPtr[x].r = srcRowPtr[2];
                        targetRowPtr[x].g = srcRowPtr[1];
                        targetRowPtr[x].b = srcRowPtr[0];
                        targetRowPtr[x].a = srcRowPtr[3];
                        srcRowPtr += 4;
                    }
                }
            }

            texture.SetPixels32(data.ColorBuffer, 0);
            texture.Apply();

            // The new hotspot is now the center of the padded texture
            hotSpot = new Vector2(W / 2f, H / 2f);

            DeleteObject(iconInfo.hbmColor);
            DeleteObject(iconInfo.hbmMask);

            return texture;
        }
    }
}
