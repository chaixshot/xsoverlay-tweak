using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using XSOverlay;
using xsoverlay_tweak.Patches.Pointer;
using xsoverlay_tweak.Utils.EventBridge;

namespace xsoverlay_tweak.Patches.Cursor
{
    [HarmonyPatch(typeof(Raycaster))]
    internal class WindowsCursorPointer
    {
        private const int CURSOR_SHOWING = 0x00000001;
        private static readonly int CURSORINFO_SIZE = Marshal.SizeOf(typeof(CURSORINFO));
        private static readonly int BITMAP_SIZE = Marshal.SizeOf(typeof(BITMAP));

        // --- CACHED SYSTEM CURSOR HANDLES ---
        private static readonly IntPtr IdentAppStarting = LoadCursor(IntPtr.Zero, (IntPtr)32650); // IDC_APPSTARTING
        private static readonly IntPtr IdentWait = LoadCursor(IntPtr.Zero, (IntPtr)32514);        // IDC_WAIT

        // Reusable buffers to reduce GC allocations
        private static byte[] _rawPixelBuffer = new byte[64 * 64 * 4];

        public class CursorData
        {
            public Texture2D CursorTexture;
            public Color32[] ColorBuffer;
            public IntPtr LastCursorHandle = IntPtr.Zero;
            public Vector2 HotSpotOffset = Vector2.zero;
            public UI_RelativeTransformManipulator RelativeTransform;
            public bool IsCursor = false;
            public uint AnimationFrame = 0;
            public float LastFrameUpdateTime = 0;
            public int LastDoubleClickState = 0;
        }

        private class XSWindowResult { public bool IsMatch; }
        public static readonly ConditionalWeakTable<Raycaster, CursorData> CursorDictionary = new();
        private static readonly ConditionalWeakTable<Unity_Overlay, XSWindowResult> IsXSWindowCache = new();

        private static readonly AccessTools.FieldRef<UI_RelativeTransformManipulator, bool> ScaleByDistance_Ref = AccessTools.FieldRefAccess<UI_RelativeTransformManipulator, bool>("ScaleByDistance");
        private static readonly AccessTools.FieldRef<Raycaster, bool> CursorLocked_Ref = AccessTools.FieldRefAccess<Raycaster, bool>("CursorLocked");

        private static int doubleClickState = 0;
        private static float doubleClickStartTime = 0f;
        private static CancellationTokenSource doubleClickCts;
        private const int markAnimationDuration = 400;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void ListenDoubleClick()
        {
            Clicker.OnClick += (instance, ref desktopCoordinate, button, holdingTouch, isDoubleClickWin, isDoubleClickXSO) =>
            {
                if (isDoubleClickWin || isDoubleClickXSO)
                {
                    doubleClickCts?.Cancel();
                    doubleClickCts = new CancellationTokenSource();
                    CancellationToken token = doubleClickCts.Token;

                    Task.Run(async () =>
                    {
                        try
                        {
                            doubleClickStartTime = Time.unscaledTime;
                            doubleClickState = 1;
                            if (isDoubleClickXSO)
                                await Task.Delay(Clicker.WinDoubleClickTime, token);
                            doubleClickStartTime = Time.unscaledTime;
                            doubleClickState = 2;

                            await Task.Delay(markAnimationDuration, token);
                            doubleClickState = 0;
                        }
                        catch (OperationCanceledException)
                        {
                            doubleClickState = 0;
                        }
                    }, token);
                }
            };
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartRaycasterInstance(Raycaster __instance)
        {
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            if (IsEnable())
                CursorDictionary.Add(__instance, new());

            XConfig.WindowsCursorPointer.SettingChanged += (Event, Args) =>
            {
                if (IsEnable())
                {
                    if (!CursorDictionary.TryGetValue(__instance, out _))
                        CursorDictionary.Add(__instance, new());
                }
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
                {
                    if (!CursorDictionary.TryGetValue(__instance, out _))
                        CursorDictionary.Add(__instance, new());
                }
                else
                    if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                    {
                        UnityEngine.Object.Destroy(Data.CursorTexture);
                        CursorDictionary.Remove(__instance);
                    }
            };
        }

        [HarmonyPatch("PreparePointerFrame")]
        [HarmonyPostfix]
        public static void ChangePointerTextureToWindowsCursor(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementOverlay, Texture2D ___CursorIcon)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
            {
                Unity_Overlay targetOverlay = __instance.HoveringOverlay;

                if (EventBridge.IsActiveHand(__instance) && __instance.HeldOverlay == null && EventBridge.IsOverlayDesktopCapture(targetOverlay) && IsTargetWindow(targetOverlay))
                {
                    if (Data.IsCursor)
                    {
                        ___VisualCursorElementOverlay.colorTint = Color.white;
                        ___VisualCursorElementOverlay.overlay.overlayColor = Color.white;
                        ___VisualCursorElementOverlay.overlay.overlayRenderModelColor = Color.white;
                        ___VisualCursorElementOverlay.AutoUpdateOverlayTexture = false;
                    }

                    if (doubleClickState != Data.LastDoubleClickState || Time.unscaledTime - Data.LastFrameUpdateTime > 0.066f || ___VisualCursorElementOverlay.overlayTexture != Data.CursorTexture) // ~15 FPS
                    {
                        Data.LastFrameUpdateTime = Time.unscaledTime;
                        CURSORINFO ci = new() { cbSize = CURSORINFO_SIZE };

                        if (GetCursorInfo(out ci) && (ci.flags & CURSOR_SHOWING) != 0)
                        {
                            bool isAnimated = IsPossiblyAnimatedCursor(ci.hCursor) || doubleClickState != 0 || Data.LastDoubleClickState != 0;
                            bool isNewCursorHandle = ci.hCursor != Data.LastCursorHandle;
                            bool shouldUpdate = isNewCursorHandle || isAnimated || Data.CursorTexture == null || ___VisualCursorElementOverlay.overlayTexture != Data.CursorTexture;

                            if (shouldUpdate)
                            {
                                Texture2D prevCursorTexture = Data.CursorTexture;

                                if (isNewCursorHandle)
                                    Data.AnimationFrame = 0;

                                Data.IsCursor = true;
                                Data.LastCursorHandle = ci.hCursor;

                                // Force software overrides any custom textures the engine loaded
                                Data.CursorTexture = ExtractCurrentWindowsCursor(ci.hCursor, Data, out Data.HotSpotOffset);
                                Data.LastDoubleClickState = doubleClickState;

                                // Disable pointer scale by distance
                                if (Data.RelativeTransform == null)
                                    Data.RelativeTransform = ___VisualCursorElementOverlay.GetComponent<UI_RelativeTransformManipulator>();

                                ScaleByDistance_Ref(Data.RelativeTransform) = false;

                                float Width = targetOverlay.renderTexWidthOverride;
                                float Height = targetOverlay.renderTexHeightOverride;
                                float targetOverlayWidthInMeters = targetOverlay.widthInMeters;
                                float dist;

                                if (Height > Width)
                                    targetOverlayWidthInMeters *= Height / Width;

                                if (targetOverlay.IsAttachedToDevice && targetOverlay.WorldSpaceSceneImpostor != null)
                                    dist = Vector3.Distance(Data.RelativeTransform._RelativeTarget.transform.position, targetOverlay.WorldSpaceSceneImpostor.transform.position);
                                else
                                    dist = Vector3.Distance(Data.RelativeTransform._RelativeTarget.transform.position, targetOverlay.transform.position);

                                float widthInMeters = 0.024f * Mathf.Clamp(dist * 0.5f, 1f, 10f) * targetOverlayWidthInMeters * PointerScaleMultiply.GetScale();
                                ___VisualCursorElementOverlay.overlayTexture = Data.CursorTexture;
                                ___VisualCursorElementOverlay.overlay.overlayTexture = Data.CursorTexture;
                                ___VisualCursorElementOverlay.widthInMeters = widthInMeters;
                                ___VisualCursorElementOverlay.overlay.overlayWidthInMeters = widthInMeters;

                                // Immediately destroy the previous texture only if a new texture reference was created
                                if (prevCursorTexture != null && prevCursorTexture != Data.CursorTexture)
                                {
                                    UnityEngine.Object.DestroyImmediate(prevCursorTexture);
                                }
                            }
                        }
                        else if (Data.IsCursor)
                            ResetToDefaultCursor(__instance, ___VisualCursorElementOverlay, ___CursorIcon, Data);
                    }
                }
                else if (Data.IsCursor)
                    ResetToDefaultCursor(__instance, ___VisualCursorElementOverlay, ___CursorIcon, Data);
            }
        }

        [HarmonyPatch("AnimateCursorClick"), HarmonyPatch("AnimateCursorHold")]
        [HarmonyPostfix]
        public static void FixClickAnimationScale(Raycaster __instance, ref Unity_Overlay ___VisualCursorElementClickAnimationOverlay, Unity_Overlay ___VisualCursorElementOverlay)
        {
            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                if (Data.IsCursor)
                    ___VisualCursorElementClickAnimationOverlay.widthInMeters = ___VisualCursorElementOverlay.widthInMeters / 1.5f;
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
            data.LastCursorHandle = IntPtr.Zero;
            data.IsCursor = false;

            if (data.RelativeTransform != null)
                ScaleByDistance_Ref(data.RelativeTransform) = true;

            CursorLocked_Ref(instance) = true;
            visualOverlay.AutoUpdateOverlayTexture = true;
            visualOverlay.overlayTexture = defaultIcon;

            visualOverlay.colorTint = XSettingsManager.Instance.Settings.AccentColor;
            visualOverlay.overlay.overlayColor = XSettingsManager.Instance.Settings.AccentColor;
            visualOverlay.overlay.overlayRenderModelColor = XSettingsManager.Instance.Settings.AccentColor;
        }

        private static bool IsPossiblyAnimatedCursor(IntPtr hCursor)
        {
            if (XConfig.WindowsCursorPointer.Value == 2) // Using animated cursor option
                return true;

            // It is ONLY animated if it is the Working in Background wheel or the Full Busy wheel
            if (hCursor == IdentAppStarting || hCursor == IdentWait)
                return true;

            // Every other cursor (Arrow, IBeam, Resize, Hand, etc.) is static
            return false;
        }

        [HarmonyPatch("SendCapturedPressClick"), HarmonyPatch("SendCapturedPressDown")]
        [HarmonyPrefix]
        public static void SetCursorPositionBeforeClick(Raycaster __instance, ref Vector2 ___CapturedPressDesktopCoordinate)
        {
            if (!IsEnable()) return;
            if (!EventBridge.IsRaycasterHand(__instance)) return;
            if (EventBridge.IsOverlayWebView(__instance.HoveringOverlay)) return;

            if (CursorDictionary.TryGetValue(__instance, out CursorData Data))
                if (Data.IsCursor)
                {
                    if (EventBridge.Ref_Raycaster.TryGetDesktopCoordinate(__instance, out Vector2 desktopCoordinate))
                    {
                        MouseOperations.SetCursorPosition((int)desktopCoordinate.x, (int)desktopCoordinate.y);
                        ___CapturedPressDesktopCoordinate = desktopCoordinate;
                    }
                }
        }

        public static bool IsCursorMode(Raycaster raycaster) => CursorDictionary.TryGetValue(raycaster, out WindowsCursorPointer.CursorData Data) && Data.IsCursor;
        public static bool IsEnable() => XConfig.WindowsCursorPointer.Value != 0 && XSettingsManager.Instance.Settings.InputMethod == InputMethods.EmulateMouse;

        //## --- Win32 API Interop ---
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

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFOHEADER pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        private unsafe static Texture2D ExtractCurrentWindowsCursor(IntPtr hCursor, CursorData data, out Vector2 hotSpot)
        {
            hotSpot = Vector2.zero;
            if (!GetIconInfo(hCursor, out ICONINFO iconInfo)) return null;

            hotSpot = new Vector2(iconInfo.xHotspot, iconInfo.yHotspot);

            int w = 0, h = 0;
            bool isMonochrome = iconInfo.hbmColor == IntPtr.Zero;
            bool isAnimated = IsPossiblyAnimatedCursor(hCursor);

            if (!isMonochrome)
            {
                GetObject(iconInfo.hbmColor, BITMAP_SIZE, out BITMAP bm);
                w = bm.bmWidth;
                h = bm.bmHeight;

                int totalBytes = bm.bmWidthBytes * bm.bmHeight;
                if (_rawPixelBuffer.Length < totalBytes) _rawPixelBuffer = new byte[totalBytes];

                if (isAnimated)
                {
                    data.AnimationFrame++;

                    IntPtr hdcScreen = GetDC(IntPtr.Zero);
                    IntPtr hdcMem = CreateCompatibleDC(hdcScreen);

                    BITMAPINFOHEADER bmi = new()
                    {
                        biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                        biWidth = w,
                        biHeight = -h, // Top-down DIB
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0 // BI_RGB
                    };

                    IntPtr hbmDib = CreateDIBSection(hdcMem, ref bmi, 0, out IntPtr bitsPtr, IntPtr.Zero, 0);
                    IntPtr hOld = SelectObject(hdcMem, hbmDib);

                    if (!DrawIconEx(hdcMem, 0, 0, hCursor, w, h, data.AnimationFrame, IntPtr.Zero, 0x0003))
                    {
                        data.AnimationFrame = 0;
                        DrawIconEx(hdcMem, 0, 0, hCursor, w, h, 0, IntPtr.Zero, 0x0003);
                    }

                    Marshal.Copy(bitsPtr, _rawPixelBuffer, 0, totalBytes);

                    SelectObject(hdcMem, hOld);
                    DeleteObject(hbmDib);
                    DeleteDC(hdcMem);
                    ReleaseDC(IntPtr.Zero, hdcScreen);
                }
                else
                {
                    GetBitmapBits(iconInfo.hbmColor, totalBytes, _rawPixelBuffer);
                }
            }
            else
            {
                GetObject(iconInfo.hbmMask, BITMAP_SIZE, out BITMAP bmMask);
                w = bmMask.bmWidth;
                h = bmMask.bmHeight / 2;

                int maskBytes = bmMask.bmWidthBytes * bmMask.bmHeight;
                if (_rawPixelBuffer.Length < maskBytes) _rawPixelBuffer = new byte[maskBytes];

                GetBitmapBits(iconInfo.hbmMask, maskBytes, _rawPixelBuffer);
            }

            int hx = iconInfo.xHotspot;
            int hy = iconInfo.yHotspot;

            // CHANGED: Force the canvas resolution to ALWAYS be 64x64
            int W = 64;
            int H = 64;

            Texture2D texture = data.CursorTexture;
            if (texture == null || texture.width != W || texture.height != H)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
                data.ColorBuffer = new Color32[W * H];
            }

            Array.Clear(data.ColorBuffer, 0, data.ColorBuffer.Length);

            if (doubleClickState == 0)
            {
                int offsetX = (W / 2) - hx;
                int offsetY = (H / 2) - hy;
                int targetYBase = H - 1 - offsetY;

                if (!isMonochrome)
                {
                    GetObject(iconInfo.hbmColor, BITMAP_SIZE, out BITMAP bm);
                    int stride = bm.bmWidthBytes;

                    fixed (Color32* pColors = data.ColorBuffer)
                    fixed (byte* pPixels = _rawPixelBuffer)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int targetY = targetYBase - y;
                            // Safety boundaries check to avoid array corruption since texture size is fixed 64x64
                            if (targetY < 0 || targetY >= H) continue;

                            byte* srcRowPtr = pPixels + (y * stride);
                            Color32* targetRowPtr = pColors + (targetY * W + offsetX);

                            for (int x = 0; x < w; x++)
                            {
                                // Verify target X is inside the 64x64 canvas boundaries
                                if (offsetX + x >= 0 && offsetX + x < W)
                                {
                                    targetRowPtr[x].r = srcRowPtr[2];
                                    targetRowPtr[x].g = srcRowPtr[1];
                                    targetRowPtr[x].b = srcRowPtr[0];
                                    targetRowPtr[x].a = srcRowPtr[3];
                                }
                                srcRowPtr += 4;
                            }
                        }
                    }
                }
                else
                {
                    GetObject(iconInfo.hbmMask, BITMAP_SIZE, out BITMAP bmMask);
                    int stride = bmMask.bmWidthBytes;

                    fixed (Color32* pColors = data.ColorBuffer)
                    fixed (byte* pPixels = _rawPixelBuffer)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int targetY = targetYBase - y;
                            // Safety boundaries check to avoid array corruption since texture size is fixed 64x64
                            if (targetY < 0 || targetY >= H) continue;

                            byte* andRowPtr = pPixels + (y * stride);
                            byte* xorRowPtr = pPixels + ((y + h) * stride);
                            Color32* targetRowPtr = pColors + (targetY * W + offsetX);

                            for (int x = 0; x < w; x++)
                            {
                                // Verify target X is inside the 64x64 canvas boundaries
                                if (offsetX + x >= 0 && offsetX + x < W)
                                {
                                    int byteIdx = x / 8;
                                    int bitIdx = 7 - (x % 8);

                                    bool andBit = ((andRowPtr[byteIdx] >> bitIdx) & 1) != 0;
                                    bool xorBit = ((xorRowPtr[byteIdx] >> bitIdx) & 1) != 0;

                                    if (!andBit && !xorBit)
                                        targetRowPtr[x] = new Color32(0, 0, 0, 255);
                                    else if (andBit && xorBit)
                                        targetRowPtr[x] = new Color32(255, 255, 255, 255);
                                    else if (!andBit && xorBit)
                                        targetRowPtr[x] = new Color32(255, 255, 255, 255);
                                    else
                                        targetRowPtr[x] = new Color32(0, 0, 0, 0);
                                }
                            }
                        }
                    }
                }
            }
            else
                ApplyDoubleClickAnimation(data.ColorBuffer, doubleClickState, doubleClickStartTime);

            texture.SetPixels32(data.ColorBuffer, 0);
            texture.Apply();

            hotSpot = new Vector2(W / 2f, H / 2f);

            if (iconInfo.hbmColor != IntPtr.Zero)
                DeleteObject(iconInfo.hbmColor);
            DeleteObject(iconInfo.hbmMask);

            return texture;
        }

        private static void ApplyDoubleClickAnimation(Color32[] colorBuffer, int state, float startTime)
        {
            if (colorBuffer == null || colorBuffer.Length != 64 * 64) return;

            float elapsed = Time.unscaledTime - startTime;

            if (state == 1) // Circle loading bar animation
            {
                float duration = (Clicker.WinDoubleClickTime > 0 ? Clicker.WinDoubleClickTime : 500) / 1000f;
                float progress = Mathf.Clamp01(elapsed / duration);

                DrawCircleLoadingBar(colorBuffer, progress);
            }
            else if (state == 2) // Check mark animation
            {
                float progress = Mathf.Clamp01(elapsed / ((markAnimationDuration - 100) / 1000f));

                DrawCheckMarkAnimation(colorBuffer, progress);
            }
        }

        private static void DrawCircleLoadingBar(Color32[] buffer, float progress)
        {
            const int W = 64;
            const int H = 64;
            const float cx = 31.5f;
            const float cy = 31.5f;
            const float radius = 22f;
            const float halfThickness = 2.5f;

            Color32 arcColor = XSettingsManager.Instance.Settings.AccentColor;
            float brightness = (arcColor.r * 0.299f + arcColor.g * 0.587f + arcColor.b * 0.114f) / 255f;
            Color32 trackColor = Color.Lerp(arcColor, brightness > 0.5f ? Color.black : Color.white, 0.4f);

            arcColor.a = 255;
            trackColor.a = 255;

            float maxAngle = progress * 2f * Mathf.PI;
            int minX = Mathf.Max(0, (int)(cx - radius - halfThickness - 1));
            int maxX = Mathf.Min(W - 1, (int)(cx + radius + halfThickness + 1));
            int minY = Mathf.Max(0, (int)(cy - radius - halfThickness - 1));
            int maxY = Mathf.Min(H - 1, (int)(cy + radius + halfThickness + 1));

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float distDelta = Mathf.Abs(dist - radius);

                    if (distDelta <= halfThickness + 0.5f)
                    {
                        float edgeAlpha = Mathf.Clamp01((halfThickness + 0.5f) - distDelta);

                        float angleRad = Mathf.Atan2(dx, dy);
                        if (angleRad < 0f) angleRad += 2f * Mathf.PI;

                        int idx = y * W + x;
                        if (angleRad <= maxAngle)
                        {
                            Color32 color = arcColor;
                            color.a = (byte)(color.a * edgeAlpha);
                            buffer[idx] = AlphaBlend(buffer[idx], color);
                        }
                        else
                        {
                            Color32 color = trackColor;
                            color.a = (byte)(color.a * edgeAlpha);
                            buffer[idx] = AlphaBlend(buffer[idx], color);
                        }
                    }
                }
            }
        }

        private static void DrawCheckMarkAnimation(Color32[] buffer, float progress)
        {
            const int W = 64;
            const int H = 64;
            const float cx = 31.5f;
            const float cy = 31.5f;
            const float bgRadius = 22f;

            Color32 bgColor = XSettingsManager.Instance.Settings.AccentColor;
            float brightness = (bgColor.r * 0.299f + bgColor.g * 0.587f + bgColor.b * 0.114f) / 255f;
            Color32 checkColor = Color.Lerp(bgColor, brightness > 0.5f ? Color.black : Color.white, 0.4f);

            bgColor.a = 255;
            checkColor.a = 255;

            int minX = Mathf.Max(0, (int)(cx - bgRadius - 1));
            int maxX = Mathf.Min(W - 1, (int)(cx + bgRadius + 1));
            int minY = Mathf.Max(0, (int)(cy - bgRadius - 1));
            int maxY = Mathf.Min(H - 1, (int)(cy + bgRadius + 1));

            const float strokeThickness = 3.0f;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= bgRadius + 0.5f)
                    {
                        float edgeAlpha = Mathf.Clamp01((bgRadius + 0.5f) - dist);
                        float strokeFactor = Mathf.Clamp01((dist - (bgRadius - strokeThickness)) / 0.5f);
                        Color32 c = Color.Lerp(bgColor, checkColor, strokeFactor);

                        c.a = (byte)(c.a * edgeAlpha);
                        int idx = y * W + x;
                        buffer[idx] = AlphaBlend(buffer[idx], c);
                    }
                }
            }

            Vector2 p0 = new(21f, 31f);
            Vector2 p1 = new(28f, 22f);
            Vector2 p2 = new(43f, 39f);

            float drawProgress = Mathf.Clamp01(progress);
            float leg1Prop = 0.35f;

            if (drawProgress > 0f)
            {
                float strokeRadius = 2.0f;

                if (drawProgress <= leg1Prop)
                {
                    float t1 = drawProgress / leg1Prop;
                    Vector2 currentEnd1 = Vector2.Lerp(p0, p1, t1);
                    DrawLineSegment(buffer, p0, currentEnd1, strokeRadius, checkColor);
                }
                else
                {
                    DrawLineSegment(buffer, p0, p1, strokeRadius, checkColor);

                    float t2 = (drawProgress - leg1Prop) / (1f - leg1Prop);
                    Vector2 currentEnd2 = Vector2.Lerp(p1, p2, t2);
                    DrawLineSegment(buffer, p1, currentEnd2, strokeRadius, checkColor);
                }
            }
        }

        private static void DrawLineSegment(Color32[] buffer, Vector2 a, Vector2 b, float radius, Color32 color)
        {
            const int W = 64;
            const int H = 64;

            int minX = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - radius - 1));
            int maxX = Mathf.Min(W - 1, (int)(Mathf.Max(a.x, b.x) + radius + 1));
            int minY = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - radius - 1));
            int maxY = Mathf.Min(H - 1, (int)(Mathf.Max(a.y, b.y) + radius + 1));

            Vector2 ab = b - a;
            float sqrLen = ab.sqrMagnitude;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new(x, y);
                    float dist;

                    if (sqrLen == 0f)
                        dist = Vector2.Distance(p, a);
                    else
                    {
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLen);
                        Vector2 proj = a + t * ab;
                        dist = Vector2.Distance(p, proj);
                    }

                    if (dist <= radius + 0.5f)
                    {
                        float edgeAlpha = Mathf.Clamp01((radius + 0.5f) - dist);
                        Color32 c = color;
                        c.a = (byte)(c.a * edgeAlpha);
                        int idx = y * W + x;
                        buffer[idx] = AlphaBlend(buffer[idx], c);
                    }
                }
            }
        }

        private static Color32 AlphaBlend(Color32 dst, Color32 src)
        {
            if (src.a == 0) return dst;
            if (src.a == 255 || dst.a == 0) return src;

            float srcA = src.a / 255f;
            float dstA = dst.a / 255f;
            float outA = srcA + dstA * (1f - srcA);

            if (outA <= 0f) return new Color32(0, 0, 0, 0);

            byte r = (byte)Mathf.Clamp((src.r * srcA + dst.r * dstA * (1f - srcA)) / outA, 0, 255);
            byte g = (byte)Mathf.Clamp((src.g * srcA + dst.g * dstA * (1f - srcA)) / outA, 0, 255);
            byte b = (byte)Mathf.Clamp((src.b * srcA + dst.b * dstA * (1f - srcA)) / outA, 0, 255);
            byte a = (byte)Mathf.Clamp(outA * 255f, 0, 255);

            return new Color32(r, g, b, a);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }
    }
}
