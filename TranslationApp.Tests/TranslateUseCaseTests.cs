using System;
using System.Threading;
using System.Threading.Tasks;
using TranslationApp.Application;
using TranslationApp.Domain;
using TranslationApp.Infrastructure.Translation;
using Xunit;

namespace TranslationApp.Tests;

public class TranslateTextUseCaseTests
{
    private sealed class FakeTranslator : ITranslator
    {
        public int Calls { get; private set; }

        public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new TranslationResult("[translated]", request.From, request.To));
        }
    }

    [Fact]
    public async Task Translates_normal_text()
    {
        var fake = new FakeTranslator();
        var useCase = new TranslateTextUseCase(fake);

        var result = await useCase.ExecuteAsync(new TranslationRequest("hello", Language.English, Language.Arabic));

        Assert.Equal("[translated]", result.Text);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task Skips_translation_for_empty_text()
    {
        var fake = new FakeTranslator();
        var useCase = new TranslateTextUseCase(fake);

        await useCase.ExecuteAsync(new TranslationRequest("   ", Language.English, Language.Arabic));

        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task Skips_translation_when_languages_are_equal()
    {
        var fake = new FakeTranslator();
        var useCase = new TranslateTextUseCase(fake);

        await useCase.ExecuteAsync(new TranslationRequest("hello", Language.English, Language.English));

        Assert.Equal(0, fake.Calls);
    }
}

public class AutoDetectAndErrorHandlingTests
{
    private readonly GoogleWebTranslator _translator = new();
    private readonly TranslateTextUseCase _useCase;

    public AutoDetectAndErrorHandlingTests()
    {
        _useCase = new TranslateTextUseCase(_translator);
    }

    [Fact]
    public async Task AutoDetect_EnglishToArabic_DetectsEnglishCorrectly()
    {
        var request = new TranslationRequest("Welcome to our new application", Language.Auto, Language.Arabic);
        var result = await _useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Equal(Language.English, result.DetectedLanguage);
        Assert.Equal(Language.Arabic, result.To);
    }

    [Fact]
    public async Task AutoDetect_ArabicToEnglish_DetectsArabicCorrectly()
    {
        var request = new TranslationRequest("مرحبا بكم في تطبيقنا الجديد", Language.Auto, Language.English);
        var result = await _useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Equal(Language.Arabic, result.DetectedLanguage);
        Assert.Equal(Language.English, result.To);
    }

    [Fact]
    public async Task AutoDetect_NumbersAndSymbols_DoesNotFalselyClaimLanguage()
    {
        // نصوص تحتوي أرقام ورموز فقط دون أحرف لغوية لا يدعي اكتشاف لغة لها
        var request = new TranslationRequest("123456789 !!! ??? #@$", Language.Auto, Language.Arabic);
        var result = await _useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.Null(result.DetectedLanguage);
    }

    [Fact]
    public async Task AutoDetect_MixedText_TranslatesWithoutCrashing()
    {
        // نص مختلط إنجليزي وعربي
        var request = new TranslationRequest("Hello مرحبا world عالم", Language.Auto, Language.French);
        var result = await _useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Equal(Language.French, result.To);
    }

    [Fact]
    public async Task AutoDetect_ShortText_TranslatesWithoutCrashing()
    {
        var request = new TranslationRequest("Hi", Language.Auto, Language.Arabic);
        var result = await _useCase.ExecuteAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Equal(Language.Arabic, result.To);
    }

    [Fact]
    public async Task AutoDetect_AsTargetLanguage_ThrowsArgumentException()
    {
        var request = new TranslationRequest("Test message", Language.English, Language.Auto);
        await Assert.ThrowsAsync<ArgumentException>(() => _translator.TranslateAsync(request));
    }

    [Fact]
    public async Task ErrorHandling_TextExceeding5000Chars_ThrowsTextTooLongException()
    {
        string longText = new string('A', 5001);
        var request = new TranslationRequest(longText, Language.English, Language.Arabic);

        var ex = await Assert.ThrowsAsync<TranslationException>(() => _useCase.ExecuteAsync(request));
        Assert.Equal(TranslationFailureReason.TextTooLong, ex.Reason);
    }

    [Fact]
    public async Task ErrorHandling_EmptyOrWhitespaceText_ReturnsQuicklyWithoutCallingTranslator()
    {
        var request1 = new TranslationRequest("", Language.Auto, Language.Arabic);
        var res1 = await _useCase.ExecuteAsync(request1);
        Assert.Equal(string.Empty, res1.Text);

        var request2 = new TranslationRequest("    \t\n  ", Language.Auto, Language.Arabic);
        var res2 = await _useCase.ExecuteAsync(request2);
        Assert.Equal("    \t\n  ", res2.Text);
    }

    [Fact]
    public async Task ErrorHandling_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // إلغاء فوري

        var request = new TranslationRequest("Cancellation test", Language.English, Language.Arabic);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _useCase.ExecuteAsync(request, cts.Token));
    }
}
