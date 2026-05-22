using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using uWindowCapture;
using Valve.VR;
using XSOverlay;
using xsoverlay_tweak.Utils;

namespace xsoverlay_tweak.Patches
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
        private static readonly ConditionalWeakTable<Raycaster, CursorData> CursorDictionary = new();

        private static readonly Func<Raycaster, RayCastResult?> GetDesktopCoordinateDelegate = AccessTools.MethodDelegate<Func<Raycaster, RayCastResult?>>(AccessTools.Method(typeof(Raycaster), "GetDesktopCoordinate"));
        private static readonly AccessTools.FieldRef<UI_RelativeTransformManipulator, bool> ScaleByDistanceRef = AccessTools.FieldRefAccess<UI_RelativeTransformManipulator, bool>("ScaleByDistance");

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(Raycaster __instance)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

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
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay, ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay, ref Texture2D ___CursorIcon)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
            {
                Unity_Overlay hoveringOverlay = __instance.HoveringOverlay;
                if (EventBridge.IsActiveHand(__instance) && __instance.HeldOverlay == null && hoveringOverlay != null && hoveringOverlay.IsDesktopOrWindowCapture)
                {
                    try
                    {
                        ___VisualCursorElementOverlay.AutoUpdateOverlayTexture = false;
                        ___VisualCursorElementOverlay.colorTint = Color.white;

                        CURSORINFO ci = new() { cbSize = CURSORINFO_SIZE };
                        if (GetCursorInfo(out ci) && (ci.flags & CURSOR_SHOWING) != 0)
                        {
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

                                ScaleByDistanceRef(Data.RelativeTransform) = false;

                                float widthInMeters = (0.015f * (Data.CursorTexture.width / 32f)) * hoveringOverlay.widthInMeters * PointerScaleMultiply.GetScale();
                                ___VisualCursorElementOverlay.overlayTexture = Data.CursorTexture;
                                ___VisualCursorElementOverlay.overlay.overlayTexture = Data.CursorTexture;
                                ___VisualCursorElementOverlay.widthInMeters = widthInMeters;
                                ___VisualCursorElementOverlay.overlay.overlayWidthInMeters = widthInMeters;
                            }
                        }
                        else if
                            (Data.IsCursor) ResetToDefaultCursor(___VisualCursorElementOverlay, ___CursorIcon, Data);
                    }
                    finally { }
                }
                else if
                    (Data.IsCursor) ResetToDefaultCursor(___VisualCursorElementOverlay, ___CursorIcon, Data);
            }
        }

        private static void ResetToDefaultCursor(Unity_Overlay visualOverlay, Texture2D defaultIcon, CursorData data)
        {
            data.IsCursor = false;
            if (data.RelativeTransform != null)
                ScaleByDistanceRef(data.RelativeTransform) = true;

            visualOverlay.AutoUpdateOverlayTexture = true;
            visualOverlay.overlayTexture = defaultIcon;
        }

        [HarmonyPatch("SetVisualCursorTransform")]
        [HarmonyPostfix]
        public static void SetCursorFacingToScreen(Raycaster __instance, ref VROverlayIntersectionResults_t rayHitResults, ref GameObject ___VisualCursorElement)
        {
            if (!IsEnable()) return;
            if (!IsHand(__instance)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                if (Data.IsCursor)
                {
                    // Convert the SteamVR intersection normal (Overlay Local Space) to Unity World Space.
                    Vector3 localNormal = new(rayHitResults.vNormal.v0, rayHitResults.vNormal.v1, -rayHitResults.vNormal.v2);
                    Vector3 worldNormal = __instance.HoveringOverlay.transform.TransformDirection(localNormal);

                    // Align the cursor's forward axis (+Z) to point into the surface (away from the normal).
                    // This makes the cursor's face parallel to the hit point face.
                    // We use the overlay's 'up' vector to keep the cursor oriented correctly relative to the window.
                    ___VisualCursorElement.transform.rotation = Quaternion.LookRotation(-worldNormal, __instance.HoveringOverlay.transform.up);
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
                        RayCastResult? desktopCoordinate = GetDesktopCoordinateDelegate(__instance);
                        MouseOperations.SetCursorPosition((int)desktopCoordinate.Value.desktopCoord.x, (int)desktopCoordinate.Value.desktopCoord.y);
                    }

            return true;
        }

        private static bool IsEnable()
        {
            return XConfig.WindowsCursorPointer.Value;
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

        private static Texture2D ExtractCurrentWindowsCursor(IntPtr hCursor, CursorData data, out Vector2 hotSpot)
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
                if (texture != null) UnityEngine.Object.Destroy(texture);
                texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
                data.ColorBuffer = new Color32[W * H];
            }

            Array.Clear(data.ColorBuffer, 0, data.ColorBuffer.Length);

            int offsetX = (W / 2) - hx;
            int offsetY = (H / 2) - hy;
            int targetYBase = H - 1 - offsetY;
            int stride = bm.bmWidthBytes;

            for (int y = 0; y < h; y++)
            {
                int srcRow = y * stride;
                int targetRow = (targetYBase - y) * W + offsetX;

                for (int x = 0; x < w; x++)
                {
                    int srcIdx = srcRow + (x << 2); // x * 4 bytes (BGRA)
                    data.ColorBuffer[targetRow + x] = new Color32(
                        _rawPixelBuffer[srcIdx + 2], _rawPixelBuffer[srcIdx + 1], _rawPixelBuffer[srcIdx], _rawPixelBuffer[srcIdx + 3]);
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
