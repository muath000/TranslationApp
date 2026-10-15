using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TranslationApp.Domain;

/// <summary>
/// نموذج تفضيلات وإعدادات المستخدم الموحد للمشروع
/// </summary>
public sealed class UserPreferences
{
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;
    public AppLanguageMode AppLanguage { get; set; } = AppLanguageMode.System;
    public HotKeyDefinition HotKey { get; set; } = HotKeyDefinition.Default;
    public CursorAssistantFlags AssistantFlags { get; set; } = CursorAssistantFlags.IsEnabled | CursorAssistantFlags.PillMode | CursorAssistantFlags.HotkeyRequired;
    public SettingsFlags Settings { get; set; } = SettingsFlags.AutoDetectRtl;
    public ThemeModeFlags ThemeMode { get; set; } = ThemeModeFlags.SystemDynamic;
    public Language SourceLanguage { get; set; } = Language.Arabic;
    public Language TargetLanguage { get; set; } = Language.English;
    public List<FavoriteItem> Favorites { get; set; } = new();

    [JsonIgnore]
    public bool AlwaysOnTop
    {
        get => (Settings & SettingsFlags.AlwaysOnTop) != 0;
        set
        {
            if (value) Settings |= SettingsFlags.AlwaysOnTop;
            else Settings &= ~SettingsFlags.AlwaysOnTop;
        }
    }

    [JsonIgnore]
    public bool AutoCopy
    {
        get => (Settings & SettingsFlags.AutoCopy) != 0;
        set
        {
            if (value) Settings |= SettingsFlags.AutoCopy;
            else Settings &= ~SettingsFlags.AutoCopy;
        }
    }

    [JsonIgnore]
    public LanguagePair Languages
    {
        get => new(SourceLanguage, TargetLanguage);
        set
        {
            SourceLanguage = value.Source;
            TargetLanguage = value.Target;
        }
    }
}

public sealed record FavoriteItem(string SourceText, string TranslatedText, Language From, Language To, DateTime Timestamp);
