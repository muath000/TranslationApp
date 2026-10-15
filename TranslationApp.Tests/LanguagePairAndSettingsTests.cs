using System;
using TranslationApp.Domain;
using Xunit;

namespace TranslationApp.Tests;

public class LanguagePairAndSettingsTests
{
    [Fact]
    public void LanguagePair_Default_IsArabicToEnglish()
    {
        var pair = LanguagePair.Default;
        Assert.Equal(Language.Arabic, pair.Source);
        Assert.Equal(Language.English, pair.Target);
    }

    [Fact]
    public void LanguagePair_Swap_NormalLanguages_SwapsSuccessfully()
    {
        var pair = new LanguagePair(Language.French, Language.German);
        var swapped = pair.Swap();

        Assert.Equal(Language.German, swapped.Source);
        Assert.Equal(Language.French, swapped.Target);
    }

    [Fact]
    public void LanguagePair_Swap_AutoWithDetectedLanguage_BecomesDetectedToTarget()
    {
        var pair = new LanguagePair(Language.Auto, Language.Arabic);
        var swapped = pair.Swap(detectedLanguage: Language.English);

        Assert.Equal(Language.Arabic, swapped.Source);
        Assert.Equal(Language.English, swapped.Target);
    }

    [Fact]
    public void LanguagePair_Swap_AutoWithoutDetectedLanguage_AppliesFallback()
    {
        var pair = new LanguagePair(Language.Auto, Language.Arabic);
        var swapped = pair.Swap(detectedLanguage: null);

        Assert.Equal(Language.Arabic, swapped.Source);
        Assert.Equal(Language.English, swapped.Target);

        var pair2 = new LanguagePair(Language.Auto, Language.English);
        var swapped2 = pair2.Swap(detectedLanguage: null);

        Assert.Equal(Language.English, swapped2.Source);
        Assert.Equal(Language.Arabic, swapped2.Target);
    }

    [Fact]
    public void LanguagePair_WithSource_SameAsTarget_AdjustsTargetToAvoidCollision()
    {
        var pair = new LanguagePair(Language.Arabic, Language.English);
        var updated = pair.WithSource(Language.English);

        Assert.Equal(Language.English, updated.Source);
        Assert.Equal(Language.Arabic, updated.Target);
    }

    [Fact]
    public void LanguagePair_WithTarget_SameAsSource_AdjustsSourceToAuto()
    {
        var pair = new LanguagePair(Language.English, Language.Arabic);
        var updated = pair.WithTarget(Language.English);

        Assert.Equal(Language.Auto, updated.Source);
        Assert.Equal(Language.English, updated.Target);
    }

    [Fact]
    public void LanguagePair_FormatSourceDisplayName_HandlesAutoAndDetected()
    {
        var pair = new LanguagePair(Language.Auto, Language.English);

        string arEmpty = pair.FormatSourceDisplayName(isArabicUi: true, detectedLanguage: null);
        Assert.Equal("اكتشاف تلقائي", arEmpty);

        string enEmpty = pair.FormatSourceDisplayName(isArabicUi: false, detectedLanguage: null);
        Assert.Equal("Auto Detect", enEmpty);

        string arWithDetected = pair.FormatSourceDisplayName(isArabicUi: true, detectedLanguage: Language.French);
        Assert.Contains("Français", arWithDetected);

        string enWithDetected = pair.FormatSourceDisplayName(isArabicUi: false, detectedLanguage: Language.French);
        Assert.Contains("Français", enWithDetected);
    }

    [Fact]
    public void TranslationFailureExtensions_GetDisplayInfo_ReturnsAccurateInfo()
    {
        var netInfoAr = TranslationFailureReason.NetworkUnavailable.GetDisplayInfo(isArabicUi: true);
        Assert.Equal("\uE783", netInfoAr.Icon);
        Assert.Contains("انقطاع", netInfoAr.Title);

        var timeoutInfoEn = TranslationFailureReason.Timeout.GetDisplayInfo(isArabicUi: false);
        Assert.Equal("\uE823", timeoutInfoEn.Icon);
        Assert.Contains("Timed Out", timeoutInfoEn.Title);

        var textTooLongAr = TranslationFailureReason.TextTooLong.GetDisplayInfo(isArabicUi: true);
        Assert.Contains("5000", textTooLongAr.Message);
    }

    [Fact]
    public void TextDirectionDetector_ResolveSourceDirection_FollowsRules()
    {
        // نصوص صريحة
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.ResolveSourceDirection("مرحبا", Language.English, isArabicUi: false));
        Assert.Equal(TextDirection.LeftToRight, TextDirectionDetector.ResolveSourceDirection("Hello", Language.Arabic, isArabicUi: true));

        // نص فارغ أو حيادي في وضع Auto
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.ResolveSourceDirection("", Language.Auto, isArabicUi: true));
        Assert.Equal(TextDirection.LeftToRight, TextDirectionDetector.ResolveSourceDirection("", Language.Auto, isArabicUi: false));
    }

    [Fact]
    public void UserPreferences_AlwaysOnTop_And_AutoCopy_SyncWithSettingsFlags()
    {
        var prefs = new UserPreferences();
        Assert.False(prefs.AlwaysOnTop);
        Assert.False(prefs.AutoCopy);

        prefs.AlwaysOnTop = true;
        Assert.True(prefs.AlwaysOnTop);
        Assert.True((prefs.Settings & SettingsFlags.AlwaysOnTop) != 0);

        prefs.AutoCopy = true;
        Assert.True(prefs.AutoCopy);
        Assert.True((prefs.Settings & SettingsFlags.AutoCopy) != 0);

        prefs.AlwaysOnTop = false;
        Assert.False(prefs.AlwaysOnTop);
        Assert.False((prefs.Settings & SettingsFlags.AlwaysOnTop) != 0);
    }
}
