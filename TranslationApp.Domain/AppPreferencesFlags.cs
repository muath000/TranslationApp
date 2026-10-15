using System;

namespace TranslationApp.Domain;

/// <summary>
/// أوضاع مظهر وثيم التطبيق
/// </summary>
public enum AppThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>
/// أوضاع لغة عرض واجهة التطبيق
/// </summary>
public enum AppLanguageMode
{
    System = 0,
    Arabic = 1,
    English = 2
}

/// <summary>
/// خيارات المظهر والثيم
/// </summary>
[Flags]
public enum ThemeModeFlags : byte
{
    None          = 0,
    SystemDynamic = 1 << 0,
    Dark          = 1 << 1,
    Light         = 1 << 2
}

/// <summary>
/// إعدادات وتفضيلات التطبيق
/// </summary>
[Flags]
public enum SettingsFlags : byte
{
    None          = 0,
    IsDrawerOpen  = 1 << 0,
    AutoCopy      = 1 << 1,
    AlwaysOnTop   = 1 << 2,
    AutoDetectRtl = 1 << 3,
    SoundFeedback = 1 << 4
}

/// <summary>
/// حالات وأنماط المساعد الذكي لمؤشر الفأرة
/// </summary>
[Flags]
public enum CursorAssistantFlags : byte
{
    Disabled       = 0,
    IsEnabled      = 1 << 0,
    PillMode       = 1 << 1,
    MiniCardMode   = 1 << 2,
    HotkeyRequired = 1 << 3,
    AutoDismiss    = 1 << 4,
    AutoCopyResult = 1 << 5
}

/// <summary>
/// إجراءات وأزرار الترجمة
/// </summary>
[Flags]
public enum TranslationActionFlags : byte
{
    None          = 0,
    Translate     = 1 << 0,
    SpeakSource   = 1 << 1,
    ClearSource   = 1 << 2,
    SwapLanguages = 1 << 3,
    CopyTarget    = 1 << 4,
    SpeakTarget   = 1 << 5,
    Favorite      = 1 << 6
}

