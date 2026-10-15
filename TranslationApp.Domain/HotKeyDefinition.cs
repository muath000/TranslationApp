using System;
using System.Text;

namespace TranslationApp.Domain;

/// <summary>
/// قناع بتات لمفاتيح التحكم في اختصارات نظام Windows (مطابق لقيم Win32)
/// </summary>
[Flags]
public enum HotKeyModifiers : byte
{
    None    = 0,
    Alt     = 1 << 0,
    Control = 1 << 1,
    Shift   = 1 << 2,
    Win     = 1 << 3
}

/// <summary>
/// بنية تمثيل اختصار لوحة المفاتيح
/// </summary>
public readonly record struct HotKeyDefinition(HotKeyModifiers Modifiers, int VirtualKey)
{
    /// <summary>
    /// الاختصار الافتراضي للنظام: Ctrl + Shift + C
    /// </summary>
    public static HotKeyDefinition Default => new(HotKeyModifiers.Control | HotKeyModifiers.Shift, 0x43);

    public bool IsEmpty => Modifiers == HotKeyModifiers.None && VirtualKey == 0;

    /// <summary>
    /// صياغة نصية للاختصار لعرضه في واجهة المستخدم (مثل: Ctrl + Shift + C)
    /// </summary>
    public string DisplayText
    {
        get
        {
            if (IsEmpty) return "لا يوجد";

            var sb = new StringBuilder(24);
            if ((Modifiers & HotKeyModifiers.Control) != 0) sb.Append("Ctrl + ");
            if ((Modifiers & HotKeyModifiers.Alt) != 0) sb.Append("Alt + ");
            if ((Modifiers & HotKeyModifiers.Shift) != 0) sb.Append("Shift + ");
            if ((Modifiers & HotKeyModifiers.Win) != 0) sb.Append("Win + ");

            string keyName = GetKeyName(VirtualKey);
            sb.Append(keyName);

            return sb.ToString();
        }
    }

    private static string GetKeyName(int vk)
    {
        if (vk >= 0x41 && vk <= 0x5A)
            return ((char)vk).ToString();

        if (vk >= 0x30 && vk <= 0x39)
            return ((char)vk).ToString();

        if (vk >= 0x70 && vk <= 0x7B)
            return $"F{vk - 0x70 + 1}";

        return vk switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x1B => "Esc",
            0x2E => "Del",
            0x2D => "Ins",
            _ => $"Key({vk})"
        };
    }
}
