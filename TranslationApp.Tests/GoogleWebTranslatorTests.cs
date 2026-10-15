using TranslationApp.Domain;
using TranslationApp.Infrastructure.Translation;

namespace TranslationApp.Tests;

public class GoogleWebTranslatorTests
{
    private readonly GoogleWebTranslator _translator = new();

    [Fact]
    public async Task Translates_ShortText_Bidirectionally()
    {
        var enReq = new TranslationRequest("Good morning", Language.English, Language.Arabic);
        var enRes = await _translator.TranslateAsync(enReq);

        Assert.NotNull(enRes);
        Assert.False(string.IsNullOrWhiteSpace(enRes.Text));
        Assert.Contains("صباح", enRes.Text);

        var arReq = new TranslationRequest("شكرا جزيلا", Language.Arabic, Language.English);
        var arRes = await _translator.TranslateAsync(arReq);

        Assert.NotNull(arRes);
        Assert.False(string.IsNullOrWhiteSpace(arRes.Text));
        Assert.Contains("thank", arRes.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Translates_Arabic_With_Tashkeel_And_Harakat()
    {
        // تشكيل وحركات لغوية
        var text = "السَّلَامُ عَلَيْكُمْ وَرَحْمَةُ اللهِ وَبَرَكَاتُهُ";
        var req = new TranslationRequest(text, Language.Arabic, Language.English);

        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
        Assert.Contains("peace", res.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Translates_SpecialCharacters_Quotes_And_Symbols()
    {
        // علامات تنصيص، رموز خاصة، ورموز برمجية ورياضية
        var text = "Price: 100% & discount is $25. He said: \"Hello + Welcome\" <world> #tag @user.";
        var req = new TranslationRequest(text, Language.English, Language.Arabic);

        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
        // يجب ألا تنكسر الرموز أو تسبب خطأ (يقبل رمز النسبة المئوية اللاتيني % أو العربي ٪)
        Assert.True(res.Text.Contains("%") || res.Text.Contains("٪"));
    }

    [Fact]
    public async Task Translates_Emojis_And_Unicode()
    {
        // إيموجي ورموز يونيكود متعددة البايتات
        var text = "Artificial Intelligence is amazing 🚀🤖! Let's celebrate 🎉✨.";
        var req = new TranslationRequest(text, Language.English, Language.Arabic);

        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
        Assert.Contains("🚀", res.Text);
    }

    [Fact]
    public async Task Translates_MultiLine_Paragraphs()
    {
        // نص متعدد الأسطر مع فواصل وفقرات
        var text = "First line of the document.\nSecond line with additional instructions.\nThird line: summary and conclusion.";
        var req = new TranslationRequest(text, Language.English, Language.Arabic);

        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
    }

    [Fact]
    public async Task Translates_TechnicalCodeSnippets()
    {
        // أكواد برمجية ومصطلحات تقنية
        var text = "public void SaveData(string key, int value) { return; }";
        var req = new TranslationRequest(text, Language.English, Language.Arabic);

        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
    }

    [Fact]
    public async Task Translates_ExtremelyLongText_Without_UrlOverflow()
    {
        // نص طويل جداً (> 1200 حرف) لاختبار دعم POST وتجاوز حد الـ GET URL
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= 20; i++)
        {
            sb.Append($"Paragraph {i}: This is a comprehensive test of our translation engine to ensure complete stability across various lengths of text. ");
        }
        var longText = sb.ToString();

        var req = new TranslationRequest(longText, Language.English, Language.Arabic);
        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
        Assert.True(res.Text.Length > 200);
    }

    [Fact]
    public async Task Translates_Nearly5000Chars_Text()
    {
        // اختبار نص بطول يقارب 5000 حرف كما طلب المستخدم للتحقق من سلوك المحرك
        var sb = new System.Text.StringBuilder();
        while (sb.Length < 4800)
        {
            sb.Append("Artificial intelligence and machine learning technologies empower modern software architectures with great efficiency. ");
        }
        var text5000 = sb.ToString();

        var req = new TranslationRequest(text5000, Language.English, Language.Arabic);
        var res = await _translator.TranslateAsync(req);

        Assert.NotNull(res);
        Assert.False(string.IsNullOrWhiteSpace(res.Text));
        Assert.True(res.Text.Length > 1000);
    }

    [Fact]
    public async Task Supports_Cancellation_Via_CancellationToken()
    {
        // اختبار إلغاء العملية أثناء الإرسال للتأكد من عدم تعليق الموارد
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // إلغاء فوري مسبق

        var req = new TranslationRequest("Testing immediate cancellation", Language.English, Language.Arabic);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _translator.TranslateAsync(req, cts.Token);
        });
    }

    [Fact]
    public async Task ConcurrentRequests_Execute_Safely()
    {
        // اختبار تزامن عدة طلبات ترجمة في نفس الوقت للتأكد من أمان الـ HttpClient
        var tasks = new List<Task<TranslationResult>>();
        for (int i = 0; i < 5; i++)
        {
            var req = new TranslationRequest($"Simultaneous request number {i}", Language.English, Language.Arabic);
            tasks.Add(_translator.TranslateAsync(req));
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, results.Length);
        foreach (var r in results)
        {
            Assert.NotNull(r);
            Assert.False(string.IsNullOrWhiteSpace(r.Text));
        }
    }
}
