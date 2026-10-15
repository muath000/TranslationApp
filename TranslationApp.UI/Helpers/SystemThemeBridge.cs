using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using TranslationApp.Domain;

namespace TranslationApp.UI.Helpers;

/// <summary>
/// جسر الربط الأصلي مع نظام Windows للمظهر ولغة العرض
/// </summary>
public static class SystemThemeBridge
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38; // 2 = Mica, 4 = Mica Alt

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    private const int WM_SETTINGCHANGE = 0x001A;

    /// <summary>
    /// التحقق مما إذا كان نظام Windows الحالي في الوضع الداكن
    /// </summary>
    public static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int useLightTheme)
            {
                return useLightTheme == 0;
            }
        }
        catch { }

        return true;
    }

    /// <summary>
    /// قراءة لون التمييز الحي الخاص بنظام الويندوز (Windows Accent Color)
    /// </summary>
    public static Color GetWindowsAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accentDword)
            {
                byte r = (byte)(accentDword & 0xFF);
                byte g = (byte)((accentDword >> 8) & 0xFF);
                byte b = (byte)((accentDword >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch { }

        return Color.FromRgb(0x00, 0x67, 0xC0);
    }

    /// <summary>
    /// التحقق الدقيق من لغة عرض نظام Windows الرسمية (UI Display Language)
    /// </summary>
    public static bool IsWindowsArabicUi()
    {
        try
        {
            ushort langId = GetUserDefaultUILanguage();
            // LANG_ARABIC = 0x01
            if ((langId & 0x03FF) == 0x0001)
            {
                return true;
            }
        }
        catch { }

        try
        {
            if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase))
                return true;
            if (CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch { }

        return false;
    }

    /// <summary>
    /// مزامنة ألوان وثيم التطبيق مع نظام Windows بدقة
    /// </summary>
    public static void SyncWithSystemTheme(Window window, bool? forceDark = null)
    {
        bool isDark = forceDark ?? IsWindowsDarkMode();
        Color accent = GetWindowsAccentColor();

        // 1. تطبيق Mica و Immersive Mode على مستوى DWM
        ApplyDwmAttributes(window, isDark);

        // 2. تحديث قواميس ألوان المظهر الحية
        var res = System.Windows.Application.Current.Resources;

        if (isDark)
        {
            res["WindowBackgroundBrush"] = Brushes.Transparent;
            res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0xBE, 0x24, 0x24, 0x24));
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
            res["ControlBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0x90, 0x30, 0x30, 0x30));
            res["ControlHoverBrush"] = new SolidColorBrush(Color.FromArgb(0xD0, 0x3C, 0x3C, 0x3C));
            res["ControlPressedBrush"] = new SolidColorBrush(Color.FromArgb(0x70, 0x1E, 0x1E, 0x1E));
            res["ControlBorderBrush"] = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            res["DividerLineBrush"] = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
            res["CaptionButtonHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));
            res["CaptionButtonPressedBrush"] = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
            res["SettingsFlyoutBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0xF4, 0x20, 0x20, 0x20));
            res["SettingsOverlayScrimBrush"] = new SolidColorBrush(Color.FromArgb(0x50, 0x00, 0x00, 0x00));
        }
        else
        {
            res["WindowBackgroundBrush"] = Brushes.Transparent;
            res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0xC0, 0xFC, 0xFC, 0xFC));
            res["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(0x25, 0x00, 0x00, 0x00));
            res["ControlBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0x90, 0xF5, 0xF5, 0xF5));
            res["ControlHoverBrush"] = new SolidColorBrush(Color.FromArgb(0xD0, 0xEB, 0xEB, 0xEB));
            res["ControlPressedBrush"] = new SolidColorBrush(Color.FromArgb(0x70, 0xE0, 0xE0, 0xE0));
            res["ControlBorderBrush"] = new SolidColorBrush(Color.FromArgb(0x18, 0x00, 0x00, 0x00));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
            res["DividerLineBrush"] = new SolidColorBrush(Color.FromArgb(0x18, 0x00, 0x00, 0x00));
            res["CaptionButtonHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x14, 0x00, 0x00, 0x00));
            res["CaptionButtonPressedBrush"] = new SolidColorBrush(Color.FromArgb(0x24, 0x00, 0x00, 0x00));
            res["SettingsFlyoutBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0xF4, 0xF8, 0xF8, 0xF8));
            res["SettingsOverlayScrimBrush"] = new SolidColorBrush(Color.FromArgb(0x35, 0x00, 0x00, 0x00));
        }

        res["SystemAccentBrush"] = new SolidColorBrush(accent);
        res["SystemAccentHoverBrush"] = new SolidColorBrush(Color.FromArgb(230, accent.R, accent.G, accent.B));
    }

    private static void ApplyDwmAttributes(Window window, bool isDark)
    {
        var helper = new WindowInteropHelper(window);
        IntPtr hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        try
        {
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            var source = HwndSource.FromHwnd(hwnd);
            if (source?.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }

            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            int trueVal = 1;
            DwmSetWindowAttribute(hwnd, 1029, ref trueVal, sizeof(int)); // Windows 11 21H2 Mica

            int backdropType = 2; // Windows 11 22H2+ Mica (DWMSBT_MAINWINDOW)
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
        }
        catch { }
    }

    /// <summary>
    /// الاستماع الآمن لتغيرات ثيم ولغة ويندوز
    /// </summary>
    public static void InitializeThemeTracking(Window window, Action? onSettingChanged = null)
    {
        SyncWithSystemTheme(window);

        var helper = new WindowInteropHelper(window);
        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (msg == WM_SETTINGCHANGE)
            {
                try
                {
                    string? area = (lParam != IntPtr.Zero) ? Marshal.PtrToStringUni(lParam) : null;
                    if (area == "ImmersiveColorSet" || area == null)
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            if (onSettingChanged != null)
                            {
                                onSettingChanged.Invoke();
                            }
                            else
                            {
                                SyncWithSystemTheme(window);
                            }
                        });
                    }
                }
                catch { }
            }
            return IntPtr.Zero;
        });
    }
}

/// <summary>
/// معالج تحديد لغة العرض وتطبيق اتجاه الواجهة
/// </summary>
public static class AppLocalization
{
    /// <summary>
    /// تحديد ما إذا كانت لغة العرض الفعالة هي العربية
    /// </summary>
    public static bool ResolveIsArabic(AppLanguageMode mode) => mode switch
    {
        AppLanguageMode.Arabic => true,
        AppLanguageMode.English => false,
        _ => SystemThemeBridge.IsWindowsArabicUi()
    };
}
