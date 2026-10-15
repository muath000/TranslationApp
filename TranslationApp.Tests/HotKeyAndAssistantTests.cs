using TranslationApp.Domain;
using Xunit;

namespace TranslationApp.Tests;

public class HotKeyAndAssistantTests
{
    [Fact]
    public void HotKeyDefinition_DefaultIsCtrlShiftC()
    {
        var defaultHotKey = HotKeyDefinition.Default;

        Assert.Equal(HotKeyModifiers.Control | HotKeyModifiers.Shift, defaultHotKey.Modifiers);
        Assert.Equal(0x43, defaultHotKey.VirtualKey); // 0x43 is 'C'
        Assert.Equal("Ctrl + Shift + C", defaultHotKey.DisplayText);
    }

    [Fact]
    public void HotKeyDefinition_CustomCombination_FormatsDisplayTextCorrectly()
    {
        // Alt + T (0x54)
        var altT = new HotKeyDefinition(HotKeyModifiers.Alt, 0x54);
        Assert.Equal("Alt + T", altT.DisplayText);

        // Ctrl + Q (0x51)
        var ctrlQ = new HotKeyDefinition(HotKeyModifiers.Control, 0x51);
        Assert.Equal("Ctrl + Q", ctrlQ.DisplayText);

        // Ctrl + Alt + F1 (0x70)
        var combo = new HotKeyDefinition(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x70);
        Assert.Equal("Ctrl + Alt + F1", combo.DisplayText);
    }

    [Fact]
    public void CursorAssistantFlags_BitwiseToggles_WorkInSingleInstruction()
    {
        CursorAssistantFlags flags = CursorAssistantFlags.Disabled;

        // تفعيل الميزة ونمط الكبسولة
        flags |= CursorAssistantFlags.IsEnabled;
        flags |= CursorAssistantFlags.PillMode;

        Assert.True(flags.HasFlag(CursorAssistantFlags.IsEnabled));
        Assert.True(flags.HasFlag(CursorAssistantFlags.PillMode));
        Assert.False(flags.HasFlag(CursorAssistantFlags.MiniCardMode));

        // التبديل إلى نمط البطاقة
        flags &= ~CursorAssistantFlags.PillMode;
        flags |= CursorAssistantFlags.MiniCardMode;

        Assert.False(flags.HasFlag(CursorAssistantFlags.PillMode));
        Assert.True(flags.HasFlag(CursorAssistantFlags.MiniCardMode));
    }

    [Fact]
    public void LanguageBitmask_AllLanguagesMapCorrectly()
    {
        Assert.Equal(LanguageBitmask.Arabic, Language.Arabic.ToBitmask());
        Assert.Equal(LanguageBitmask.English, Language.English.ToBitmask());
        Assert.Equal(LanguageBitmask.French, Language.French.ToBitmask());
        Assert.Equal(LanguageBitmask.German, Language.German.ToBitmask());
        Assert.Equal(LanguageBitmask.Spanish, Language.Spanish.ToBitmask());
        Assert.Equal(LanguageBitmask.Turkish, Language.Turkish.ToBitmask());

        Assert.Equal("العربية", Language.Arabic.GetDisplayName());
        Assert.Equal("English", Language.English.GetDisplayName());
        Assert.Equal("Français", Language.French.GetDisplayName());
        Assert.Equal("Deutsch", Language.German.GetDisplayName());
        Assert.Equal("Español", Language.Spanish.GetDisplayName());
        Assert.Equal("Türkçe", Language.Turkish.GetDisplayName());
    }

    [Fact]
    public void SettingsFlags_And_ThemeModeFlags_BitwiseOperations()
    {
        SettingsFlags settings = SettingsFlags.None;
        settings |= SettingsFlags.AutoCopy | SettingsFlags.AutoDetectRtl;

        Assert.True(settings.HasFlag(SettingsFlags.AutoCopy));
        Assert.True(settings.HasFlag(SettingsFlags.AutoDetectRtl));
        Assert.False(settings.HasFlag(SettingsFlags.AlwaysOnTop));

        ThemeModeFlags theme = ThemeModeFlags.SystemDynamic;
        Assert.True(theme.HasFlag(ThemeModeFlags.SystemDynamic));
        Assert.False(theme.HasFlag(ThemeModeFlags.Dark));
    }
}
