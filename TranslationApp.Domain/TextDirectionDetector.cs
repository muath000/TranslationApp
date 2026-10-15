namespace TranslationApp.Domain;

/// <summary>
/// يمثل اتجاه النص اللغوي بناءً على أول محرف قوي
/// </summary>
public enum TextDirection
{
    Neutral,
    LeftToRight,
    RightToLeft
}

/// <summary>
/// فاحص اتجاه النص بالاعتماد على أول محرف قوي وفق قواعد يونيكود BiDi.
/// نطاقات المحارف العربية القوية: U+0600..U+06FF, U+0750..U+077F, U+FB50..U+FDFF, U+FE70..U+FEFF.
/// المحارف اللاتينية القوية: A-Z و a-z.
/// </summary>
public static class TextDirectionDetector
{
    public static TextDirection Detect(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return TextDirection.Neutral;
        }

        foreach (char c in text)
        {
            if (IsArabicStrong(c))
            {
                return TextDirection.RightToLeft;
            }

            if (IsLatinStrong(c))
            {
                return TextDirection.LeftToRight;
            }
        }

        return TextDirection.Neutral;
    }

    private static bool IsArabicStrong(char c)
    {
        return (c >= 0x0600 && c <= 0x06FF) ||
               (c >= 0x0750 && c <= 0x077F) ||
               (c >= 0x08A0 && c <= 0x08FF) ||
               (c >= 0xFB50 && c <= 0xFDFF) ||
               (c >= 0xFE70 && c <= 0xFEFF) ||
               (c >= 0x0590 && c <= 0x05FF);
    }

    private static bool IsLatinStrong(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    /// <summary>
    /// تحديد اتجاه حقل الإدخال المصدر بناءً على فحص النص واللغة المختارة ولغة الواجهة
    /// </summary>
    public static TextDirection ResolveSourceDirection(string? text, Language sourceLang, bool isArabicUi)
    {
        var dir = Detect(text);
        if (dir != TextDirection.Neutral)
        {
            return dir;
        }

        bool shouldBeRtl = (sourceLang == Language.Arabic) || (sourceLang == Language.Auto && isArabicUi);
        return shouldBeRtl ? TextDirection.RightToLeft : TextDirection.LeftToRight;
    }

    /// <summary>
    /// تحديد اتجاه حقل نتيجة الترجمة بناءً على فحص النص واللغة الهدف
    /// </summary>
    public static TextDirection ResolveTargetDirection(string? text, Language targetLang)
    {
        var dir = Detect(text);
        if (dir != TextDirection.Neutral)
        {
            return dir;
        }

        return (targetLang == Language.Arabic) ? TextDirection.RightToLeft : TextDirection.LeftToRight;
    }
}
