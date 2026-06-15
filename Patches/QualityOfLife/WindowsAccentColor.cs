using HarmonyLib;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using XSOverlay;

namespace xsoverlay_tweak.Patches.QualityOfLife
{
    internal class WindowsAccentColor
    {
        private static UnityEngine.Color? xsoColor;

        [HarmonyPatch(typeof(UpdateDateTime), "Awake")]
        [HarmonyPostfix]
        public static void Initialized()
        {
            XConfig.WindowsAccentColor.SettingChanged += (s, e) =>
            {
                if (IsEnalbe())
                    ApplyAccentColor(XSettingsManager.Instance);
                else
                    XSettingsManager.Instance.Settings.AccentColor = (UnityEngine.Color)xsoColor;
            };
        }

        [HarmonyPatch(typeof(XSettingsManager), "Awake")]
        [HarmonyPostfix]
        public static void XSettingsManagerLoaded(XSettingsManager __instance)
        {
            if (!IsEnalbe()) return;

            ApplyAccentColor(__instance);
        }

        [HarmonyPatch(typeof(XSettingsManager), nameof(XSettingsManager.ChangeColorChannel))]
        [HarmonyPostfix]
        public static void ChangeColorChannel(XSettingsManager __instance)
        {
            if (!IsEnalbe()) return;

            xsoColor = __instance.Settings.AccentColor;
        }

        private static void ApplyAccentColor(XSettingsManager instance)
        {
            Color? accentColor = GetWindowsAccentColor();

            if (accentColor.HasValue)
            {
                xsoColor ??= instance.Settings.AccentColor;

                // Use .Value to access the underlying struct's properties
                instance.Settings.AccentColor.r = accentColor.Value.R / 255f;
                instance.Settings.AccentColor.g = accentColor.Value.G / 255f;
                instance.Settings.AccentColor.b = accentColor.Value.B / 255f;
                // instance.Settings.AccentColor.a = accentColor.Value.A / 255f;
            }
        }

        // Native Win32 API structure fallback for Windows 7, 7.1, 8, and 8.1
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmGetColorizationColor(out uint pcrColorization, [MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

        private static Color? GetWindowsAccentColor()
        {
            try
            {
                // Windows 10 & 11 Path (Registry)
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key != null)
                {
                    object accentColorObj = key.GetValue("AccentColor");
                    if (accentColorObj is int accentColorDword)
                    {
                        // Registry layout holds ABGR integers on Win 10/11
                        byte r = (byte)(accentColorDword & 0xFF);
                        byte g = (byte)((accentColorDword >> 8) & 0xFF);
                        byte b = (byte)((accentColorDword >> 16) & 0xFF);
                        byte a = (byte)((accentColorDword >> 24) & 0xFF);

                        return Color.FromArgb(a, r, g, b);
                    }
                }

                // Windows 7, 7.1, 8, & 8.1 Fallback Path (DWM Engine API)
                // This natively queries the Aero Glass / Metro Colorization frame tint
                DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);

                // DwmGetColorizationColor returns raw bytes mapped as ARGB layout 
                byte win7A = (byte)((colorizationColor >> 24) & 0xFF);
                byte win7R = (byte)((colorizationColor >> 16) & 0xFF);
                byte win7G = (byte)((colorizationColor >> 8) & 0xFF);
                byte win7B = (byte)(colorizationColor & 0xFF);

                return Color.FromArgb(win7A, win7R, win7G, win7B);
            }
            catch (Exception ex)
            {
                // Safely log the environment failure and return null as requested
                Plugin.Logger.LogError($"Error getting accent color across OS targets: {ex.Message}");
                return null;
            }
        }

        private static bool IsEnalbe()
        {
            return XConfig.WindowsAccentColor.Value;
        }
    }
}
