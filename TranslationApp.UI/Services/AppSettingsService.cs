using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TranslationApp.Domain;

namespace TranslationApp.UI.Services;


/// <summary>
/// خدمة إدارة وحفظ التفضيلات والإعدادات المركزية
/// </summary>
public static class AppSettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TranslationApp");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "preferences.json");

    public static UserPreferences Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var prefs = JsonSerializer.Deserialize<UserPreferences>(json);
                if (prefs != null)
                {
                    // التوافق العكسي: إذا كان الثيم محفوظاً سابقاً في قناع ThemeMode
                    if (prefs.Theme == AppThemeMode.System && prefs.ThemeMode != ThemeModeFlags.SystemDynamic)
                    {
                        if ((prefs.ThemeMode & ThemeModeFlags.Dark) != 0) prefs.Theme = AppThemeMode.Dark;
                        else if ((prefs.ThemeMode & ThemeModeFlags.Light) != 0) prefs.Theme = AppThemeMode.Light;
                    }
                    return prefs;
                }
            }
        }
        catch { }

        return new UserPreferences();
    }

    public static void Save(UserPreferences prefs)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            // مزامنة الحقل القديم لضمان التوافق مع أي مكونات سابقة
            prefs.ThemeMode = prefs.Theme switch
            {
                AppThemeMode.Dark => ThemeModeFlags.Dark,
                AppThemeMode.Light => ThemeModeFlags.Light,
                _ => ThemeModeFlags.SystemDynamic
            };

            string json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });

            // الحفظ الذري لمنع تلف الملف عند الإغلاق المفاجئ
            string tempFile = Path.Combine(SettingsDirectory, "preferences.tmp");
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, SettingsFilePath, overwrite: true);
        }
        catch { }
    }
}
