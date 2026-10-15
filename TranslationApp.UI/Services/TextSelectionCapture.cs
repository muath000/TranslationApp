using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace TranslationApp.UI.Services;

/// <summary>
/// خدمة التقاط النص المحدد وموقع مؤشر الفأرة من أي تطبيق نشط في ويندوز (Zero-Lag Text Capture)
/// </summary>
public static class TextSelectionCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_SHIFT = 0x10;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU = 0x12;
    private const byte VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// الحصول على إحداثيات مؤشر الفأرة الحالية على مستوى شاشة ويندوز
    /// </summary>
    public static Point GetCurrentMousePosition()
    {
        if (GetCursorPos(out POINT p))
        {
            return new Point(p.X, p.Y);
        }
        return new Point(100, 100);
    }

    /// <summary>
    /// محاكاة نسخ النص المحدد من التطبيق النشط واستخراجه من الـ Clipboard بأمان تام
    /// </summary>
    public static async Task<string> TryCaptureSelectedTextAsync()
    {
        try
        {
            // 1. مسح الحافظة للتأكد من وصول نص جديد محدد فعلياً
            try { Clipboard.Clear(); } catch { }

            // 2. تحرير مفاتيح Shift و Alt إن كانت مضغوطة من الاختصار لمنع تعارض الأوامر في المتصفحات
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // 3. محاكاة إرسال Ctrl + C للتطبيق النشط حالياً
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // 4. استطلاع الحافظة مع إعادة المحاولة لتجنب قفل الحافظة المؤقت (CLIPBRD_E_CANT_OPEN)
            for (int retry = 0; retry < 5; retry++)
            {
                await Task.Delay(40);
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText().Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
                catch (COMException) { }
                catch (ExternalException) { }
            }
        }
        catch { }

        return string.Empty;
    }
}
