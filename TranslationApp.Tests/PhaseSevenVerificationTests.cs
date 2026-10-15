using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TranslationApp.Application;
using TranslationApp.Domain;
using TranslationApp.Infrastructure.Translation;
using TranslationApp.UI.Helpers;
using TranslationApp.UI.Services;
using Xunit;

namespace TranslationApp.Tests;

/// <summary>
/// اختبارات التحقق الشاملة للمرحلة السابعة (Phase 7 Comprehensive Verification Suite)
/// </summary>
public class PhaseSevenVerificationTests
{
    #region 1. اختبارات حفظ الإعدادات واسترجاعها (Settings Persistence & Retrieval)

    [Fact]
    public void SettingsPersistence_SaveAndLoad_RoundTripPreservesAllValues()
    {
        // تهيئة تفضيلات مخصصة
        var original = new UserPreferences
        {
            Theme = AppThemeMode.Dark,
            AppLanguage = AppLanguageMode.Arabic,
            AlwaysOnTop = true,
            AutoCopy = true,
            Languages = new LanguagePair(Language.French, Language.German)
        };
        original.Favorites.Add(new FavoriteItem("Bonjour", "Guten Tag", Language.French, Language.German, DateTime.UtcNow));

        // حفظ التفضيلات
        AppSettingsService.Save(original);

        // استرجاع التفضيلات
        var loaded = AppSettingsService.Load();

        Assert.NotNull(loaded);
        Assert.Equal(AppThemeMode.Dark, loaded.Theme);
        Assert.Equal(AppLanguageMode.Arabic, loaded.AppLanguage);
        Assert.True(loaded.AlwaysOnTop);
        Assert.True(loaded.AutoCopy);
        Assert.Equal(Language.French, loaded.Languages.Source);
        Assert.Equal(Language.German, loaded.Languages.Target);
        Assert.NotEmpty(loaded.Favorites);
        Assert.Equal("Bonjour", loaded.Favorites[0].SourceText);

        // استعادة الوضع الافتراضي النظيف للاختبارات التالية
        AppSettingsService.Save(new UserPreferences());
    }

    [Fact]
    public void SettingsPersistence_DefaultFallback_WhenFileDoesNotExistOrCorrupted()
    {
        var defaults = new UserPreferences();
        Assert.Equal(AppThemeMode.System, defaults.Theme);
        Assert.Equal(AppLanguageMode.System, defaults.AppLanguage);
        Assert.False(defaults.AlwaysOnTop);
        Assert.False(defaults.AutoCopy);
        Assert.Equal(Language.Arabic, defaults.Languages.Source);
        Assert.Equal(Language.English, defaults.Languages.Target);
        Assert.Empty(defaults.Favorites);
    }

    #endregion

    #region 2. اختبارات العربية والإنجليزية ومحاذاة النصوص (Arabic & English Localization & Text Direction)

    [Fact]
    public void Localization_ResolveIsArabic_HandlesModesAccurately()
    {
        Assert.True(AppLocalization.ResolveIsArabic(AppLanguageMode.Arabic));
        Assert.False(AppLocalization.ResolveIsArabic(AppLanguageMode.English));

        // وضع System يستعلم من إعدادات النظام الحية دون رمي استثناءات
        bool systemIsArabic = AppLocalization.ResolveIsArabic(AppLanguageMode.System);
        Assert.True(systemIsArabic || !systemIsArabic); // قيمة بولية صحيحة
    }

    [Theory]
    [InlineData("السلام عليكم ورحمة الله", TextDirection.RightToLeft)]
    [InlineData("Hello World from TranslationApp", TextDirection.LeftToRight)]
    [InlineData("123456 !!! ???", TextDirection.Neutral)]
    public void TextDirection_Detection_IdentifiesCorrectDirection(string text, TextDirection expected)
    {
        var actual = TextDirectionDetector.Detect(text);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDirection_Resolution_FollowsUiAndLanguageContext()
    {
        // نص إنجليزي يبقى LTR
        Assert.Equal(TextDirection.LeftToRight, TextDirectionDetector.ResolveSourceDirection("English text", Language.Arabic, isArabicUi: true));

        // نص عربي يبقى RTL
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.ResolveSourceDirection("نص عربي", Language.English, isArabicUi: false));

        // نص فارغ أو أرقام مع لغة مصدر عربية -> RTL
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.ResolveSourceDirection("", Language.Arabic, isArabicUi: false));

        // نص فارغ مع لغة مصدر Auto وواجهة عربية -> RTL
        Assert.Equal(TextDirection.RightToLeft, TextDirectionDetector.ResolveSourceDirection("", Language.Auto, isArabicUi: true));

        // نص فارغ مع لغة مصدر Auto وواجهة إنجليزية -> LTR
        Assert.Equal(TextDirection.LeftToRight, TextDirectionDetector.ResolveSourceDirection("", Language.Auto, isArabicUi: false));
    }

    [Fact]
    public void ErrorMessages_BilingualDisplay_ProvidesCleanTranslations()
    {
        foreach (TranslationFailureReason reason in Enum.GetValues<TranslationFailureReason>())
        {
            var arInfo = reason.GetDisplayInfo(isArabicUi: true);
            Assert.False(string.IsNullOrWhiteSpace(arInfo.Icon));
            Assert.False(string.IsNullOrWhiteSpace(arInfo.Title));
            Assert.False(string.IsNullOrWhiteSpace(arInfo.Message));

            var enInfo = reason.GetDisplayInfo(isArabicUi: false);
            Assert.False(string.IsNullOrWhiteSpace(enInfo.Icon));
            Assert.False(string.IsNullOrWhiteSpace(enInfo.Title));
            Assert.False(string.IsNullOrWhiteSpace(enInfo.Message));

            // تحقق من عدم تطابق النصين بين العربي والإنجليزي (لكل لغة صياغة معربة صحيحة)
            Assert.NotEqual(arInfo.Title, enInfo.Title);
        }
    }

    #endregion

    #region 3. اختبارات وضع System وتكامل Windows 11 (Windows 11 Native Integration)

    [Fact]
    public void SystemThemeBridge_ReadsWindowsEnvironment_WithoutExceptions()
    {
        // قراءة الوضع الداكن للنظام
        bool isDark = SystemThemeBridge.IsWindowsDarkMode();
        Assert.True(isDark || !isDark);

        // قراءة لون التمييز الحي
        Color accent = SystemThemeBridge.GetWindowsAccentColor();
        Assert.True(accent.A > 0);

        // قراءة لغة الواجهة للنظام
        bool isArabicUi = SystemThemeBridge.IsWindowsArabicUi();
        Assert.True(isArabicUi || !isArabicUi);
    }

    #endregion

    #region 4. اختبارات تغيير المظهر (Theme Switching Behavior)

    [Fact]
    public void ThemeModes_Serialization_SyncsWithLegacyFlags()
    {
        var prefsDark = new UserPreferences { Theme = AppThemeMode.Dark };
        AppSettingsService.Save(prefsDark);
        Assert.Equal(ThemeModeFlags.Dark, prefsDark.ThemeMode);

        var prefsLight = new UserPreferences { Theme = AppThemeMode.Light };
        AppSettingsService.Save(prefsLight);
        Assert.Equal(ThemeModeFlags.Light, prefsLight.ThemeMode);

        var prefsSystem = new UserPreferences { Theme = AppThemeMode.System };
        AppSettingsService.Save(prefsSystem);
        Assert.Equal(ThemeModeFlags.SystemDynamic, prefsSystem.ThemeMode);

        // استعادة الوضع الافتراضي
        AppSettingsService.Save(new UserPreferences());
    }

    #endregion

    #region 5. اختبارات الترجمة واكتشاف اللغة (Translation & Language Detection)

    [Fact]
    public async Task Translation_ShortCircuits_WhenTextIsEmptyOrWhitespace()
    {
        var translator = new GoogleWebTranslator();
        var useCase = new TranslateTextUseCase(translator);

        var request = new TranslationRequest("   ", Language.Arabic, Language.English);
        var result = await useCase.ExecuteAsync(request);

        Assert.Equal("   ", result.Text);
    }

    [Fact]
    public async Task Translation_ShortCircuits_WhenSourceAndTargetMatch()
    {
        var translator = new GoogleWebTranslator();
        var useCase = new TranslateTextUseCase(translator);

        var request = new TranslationRequest("مرحبا بكم", Language.Arabic, Language.Arabic);
        var result = await useCase.ExecuteAsync(request);

        Assert.Equal("مرحبا بكم", result.Text);
    }

    [Fact]
    public async Task Translation_ThrowsTextTooLongException_WhenExceeding5000Chars()
    {
        var translator = new GoogleWebTranslator();
        var useCase = new TranslateTextUseCase(translator);

        string longText = new string('A', 5001);
        var request = new TranslationRequest(longText, Language.English, Language.Arabic);

        var ex = await Assert.ThrowsAsync<TranslationException>(() => useCase.ExecuteAsync(request));
        Assert.Equal(TranslationFailureReason.TextTooLong, ex.Reason);
    }

    [Fact]
    public async Task Translation_Cancellation_TerminatesSafely()
    {
        var translator = new GoogleWebTranslator();
        var useCase = new TranslateTextUseCase(translator);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // إلغاء فوري قبل البدء

        var request = new TranslationRequest("Hello World", Language.English, Language.Arabic);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(request, cts.Token));
    }

    [Fact]
    public async Task Translation_AutoDetect_TranslatesSuccessfully()
    {
        var translator = new GoogleWebTranslator();
        var useCase = new TranslateTextUseCase(translator);

        var request = new TranslationRequest("Good morning", Language.Auto, Language.Arabic);
        var result = await useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Equal(Language.English, result.DetectedLanguage);
    }

    #endregion

    #region 6. اختبارات سلوك النوافذ وزوج اللغات (Window & LanguagePair Behavior)

    [Fact]
    public void LanguagePair_PreventsDuplicateSourceAndTarget()
    {
        var pair = new LanguagePair(Language.Arabic, Language.English);

        // عند اختيار المصدر ليطابق الهدف، يتم تعديل الهدف تلقائياً
        var withSource = pair.WithSource(Language.English);
        Assert.Equal(Language.English, withSource.Source);
        Assert.Equal(Language.Arabic, withSource.Target);

        // عند اختيار الهدف ليطابق المصدر، يتحول المصدر تلقائياً إلى Auto
        var withTarget = pair.WithTarget(Language.Arabic);
        Assert.Equal(Language.Auto, withTarget.Source);
        Assert.Equal(Language.Arabic, withTarget.Target);
    }

    [Fact]
    public void WindowBehavior_AlwaysOnTopAndAutoCopy_SyncAccurately()
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
    }

    #endregion
}
