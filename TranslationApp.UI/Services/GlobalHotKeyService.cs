using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TranslationApp.Domain;

namespace TranslationApp.UI.Services;

/// <summary>
/// خدمة تسجيل واستماع الاختصارات العامة على مستوى نظام ويندوز (Win32 Global HotKey Service)
/// كائنية التوجه ومسؤولية واحدة (SRP): مسؤولة حصراً عن الاتصال مع نواة الويندوز واستقبال WM_HOTKEY
/// </summary>
public sealed class GlobalHotKeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _source;
    private bool _isRegistered;
    private HotKeyDefinition _currentHotKey;

    public event Action? HotKeyPressed;

    public HotKeyDefinition CurrentHotKey => _currentHotKey;
    public bool IsRegistered => _isRegistered;

    public void Initialize(Window window, HotKeyDefinition initialHotKey)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero)
        {
            window.SourceInitialized += (_, _) =>
            {
                _hwnd = new WindowInteropHelper(window).Handle;
                HookHwnd();
                Register(initialHotKey);
            };
        }
        else
        {
            HookHwnd();
            Register(initialHotKey);
        }
    }

    private void HookHwnd()
    {
        if (_hwnd != IntPtr.Zero && _source == null)
        {
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProcHook);
        }
    }

    public bool Register(HotKeyDefinition hotKey)
    {
        if (_hwnd == IntPtr.Zero) return false;
        if (hotKey.IsEmpty) return false;

        var previousHotKey = _currentHotKey;
        bool wasRegistered = _isRegistered;

        Unregister();

        uint modifiers = (uint)hotKey.Modifiers;
        uint vk = (uint)hotKey.VirtualKey;

        // إضافة MOD_NOREPEAT لمنع تكرار الإطلاق عند استمرار الضغط
        modifiers |= 0x4000;

        bool success = RegisterHotKey(_hwnd, HOTKEY_ID, modifiers, vk);
        if (success)
        {
            _isRegistered = true;
            _currentHotKey = hotKey;
            return true;
        }

        // إذا فشل تسجيل الاختصار الجديد، استعادة الاختصار السابق لحماية الخدمة من التوقف
        if (wasRegistered && !previousHotKey.IsEmpty)
        {
            uint prevMod = (uint)previousHotKey.Modifiers | 0x4000;
            uint prevVk = (uint)previousHotKey.VirtualKey;
            if (RegisterHotKey(_hwnd, HOTKEY_ID, prevMod, prevVk))
            {
                _isRegistered = true;
                _currentHotKey = previousHotKey;
            }
        }

        return false;
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero && _isRegistered)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotKeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_source != null)
        {
            _source.RemoveHook(WndProcHook);
            _source = null;
        }
    }
}
