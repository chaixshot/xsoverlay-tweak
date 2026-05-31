using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace xsoverlay_tweak.Utils
{
    public unsafe class MouseInputDetector : NativeWindow, IDisposable
    {
        // Event to notify your main application
        public event Action<int, int> PhysicalMouseMoved;

        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        private const int MOVEMENT_THRESHOLD = 2; // Jitter threshold to filter out micro-movements

        // Cache sizes and buffers to avoid allocations in the loop
        private readonly uint _headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
        private byte[] _inputBuffer = new byte[128];

        public MouseInputDetector()
        {
            CreateHandle(new CreateParams());
            RegisterDevice(this.Handle);
        }

        private void RegisterDevice(IntPtr targetHandle)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = targetHandle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new Exception("Failed to register Raw Input device.");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                uint dwSize = (uint)_inputBuffer.Length;

                // 'fixed' prevents the GC from moving our buffer while we read it
                fixed (byte* pBuffer = _inputBuffer)
                {
                    int result = GetRawInputData(
                        m.LParam,
                        RID_INPUT,
                        (IntPtr)pBuffer,
                        ref dwSize,
                        _headerSize);

                    if (result >= 0)
                    {
                        // Direct pointer casting (Zero overhead)
                        RAWINPUTHEADER* header = (RAWINPUTHEADER*)pBuffer;

                        // hDevice != 0 means it's a physical HID device (not simulated)
                        if (header->hDevice != IntPtr.Zero)
                        {
                            // Move pointer forward past the header to the mouse data
                            RAWMOUSE* mouse = (RAWMOUSE*)(pBuffer + _headerSize);

                            // Performance Optimization: Only notify movement if deltas exceed threshold
                            if (Math.Abs(mouse->lLastX) >= MOVEMENT_THRESHOLD || Math.Abs(mouse->lLastY) >= MOVEMENT_THRESHOLD)
                                PhysicalMouseMoved?.Invoke(mouse->lLastX, mouse->lLastY);
                        }
                    }
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
            _inputBuffer = null;
        }

        #region Ultra-Fast Win32 Structs (Sequential)
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        #endregion
    }
}