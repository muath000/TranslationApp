using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TranslationApp.Application;
using TranslationApp.Domain;
using Xunit;

namespace TranslationApp.Tests;

public class SmartTextSplitterTests
{
    private const string SampleStoryFromScreenshot = @"fan! I love your music and I go to all your concerts!""

“No problem at all,” said the lead-singer of the grasshoppers. “Flattery will get you everywhere!”

“Oh good,” said the donkey, “because I have one little request.”

“Sorry, we don’t do requests. We only have one tune, and the one that goes crickets,crickets, crickets.”

“Oh yes, that will always be my personal number one,” said the donkey. “But my request is of a different nature. I want you to teach me to sing.”

“Certainly,” said the grasshopper. The more who can sing the merrier. Try this, “DO, RE, MI, FA, SOL, LA, TI.”";

    [Fact]
    public void Split_ShortText_ReturnsSingleChunkWithNoSuffix()
    {
        string shortText = "Hello world!";
        var chunks = SmartTextSplitter.Split(shortText, maxChunkSize: 380);

        Assert.Single(chunks);
        Assert.Equal("Hello world!", chunks[0].Content);
        Assert.Equal(string.Empty, chunks[0].Suffix);
    }

    [Fact]
    public void Split_EmptyOrNull_ReturnsEmptyList()
    {
        Assert.Empty(SmartTextSplitter.Split(string.Empty));
        Assert.Empty(SmartTextSplitter.Split(null!));
    }

    [Fact]
    public void Split_SampleStory_AllChunksUnderMaxLimit()
    {
        // نكرر القصة لنحصل على نص بطول 2500+ حرف مطابق تماماً لما ظهر في صورة المستخدم
        string longStory = string.Join("\n\n", Enumerable.Repeat(SampleStoryFromScreenshot, 4));
        Assert.True(longStory.Length > 2000, "Text should be long like in screenshot");

        var chunks = SmartTextSplitter.Split(longStory, maxChunkSize: 380);

        Assert.True(chunks.Count > 1, "Should be split into multiple chunks");

        foreach (var chunk in chunks)
        {
            Assert.True(chunk.Content.Length <= 380, $"Chunk '{chunk.Content.Substring(0, Math.Min(30, chunk.Content.Length))}...' length {chunk.Content.Length} exceeds 380");
        }
    }

    [Fact]
    public void Split_ReconstructsExactOriginalTextWithoutLoss()
    {
        string longStory = string.Join("\n\n", Enumerable.Repeat(SampleStoryFromScreenshot, 3));

        var chunks = SmartTextSplitter.Split(longStory, maxChunkSize: 380);

        var reconstructed = string.Concat(chunks.Select(c => c.Content + c.Suffix));

        Assert.Equal(longStory, reconstructed);
    }

    [Fact]
    public void Split_ExtremelyLongContinuousWord_SplitsSafely()
    {
        string longWord = new string('A', 1000);
        var chunks = SmartTextSplitter.Split(longWord, maxChunkSize: 380);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(380, chunks[0].Content.Length);
        Assert.Equal(380, chunks[1].Content.Length);
        Assert.Equal(240, chunks[2].Content.Length);

        var reconstructed = string.Concat(chunks.Select(c => c.Content + c.Suffix));
        Assert.Equal(longWord, reconstructed);
    }

    [Fact]
    public async Task TranslateTextUseCase_SplitsLongTextAndRecombinesBehindTheScenes()
    {
        var mockTranslator = new FakeEchoTranslator();
        var useCase = new TranslateTextUseCase(mockTranslator);

        string longStory = string.Join("\n\n", Enumerable.Repeat(SampleStoryFromScreenshot, 4));
        var request = new TranslationRequest(longStory, Language.English, Language.Arabic);

        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));

        // التحقق من استدعاء المترجم لعدة مقاطع مجتزأة خلف الكواليس
        Assert.True(mockTranslator.TranslatedQueries.Count > 1, "Translator should have been called for multiple chunks");

        // التأكد من أن كل مقطع تم إرساله للمترجم لا يتجاوز 380 حرف
        foreach (var query in mockTranslator.TranslatedQueries)
        {
            Assert.True(query.Length <= 380, $"Query '{query.Substring(0, Math.Min(20, query.Length))}...' was {query.Length} chars (> 380)");
        }

        // التأكد من أن الناتج المعاد تجميعه يحتوي على نفس عدد الفقرات
        int originalParagraphs = longStory.Split(new[] { "\n\n" }, StringSplitOptions.None).Length;
        int translatedParagraphs = result.Text.Split(new[] { "\n\n" }, StringSplitOptions.None).Length;
        Assert.Equal(originalParagraphs, translatedParagraphs);
    }

    private class FakeEchoTranslator : ITranslator
    {
        public List<string> TranslatedQueries { get; } = new();

        public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default)
        {
            TranslatedQueries.Add(request.Text);
            // محاكاة ترجمة وهمية تضيف علامة مترجم
            string translated = "[AR] " + request.Text;
            return Task.FromResult(new TranslationResult(translated, request.From, request.To));
        }
    }
}
