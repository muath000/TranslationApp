using System.Text;
using TranslationApp.Domain;

namespace TranslationApp.Application;

public interface ITranslator
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default);
}

public sealed class TranslateTextUseCase
{
    private readonly ITranslator _translator;

    public TranslateTextUseCase(ITranslator translator)
    {
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public async Task<TranslationResult> ExecuteAsync(TranslationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Text != null && request.Text.Length > 5000)
        {
            throw new TranslationException(TranslationFailureReason.TextTooLong, "النص يتجاوز الحد الأقصى المسموح به (5000 حرف).");
        }

        if (IsTranslationUnnecessary(request))
        {
            return ReturnOriginalText(request);
        }

        return await PerformTranslationAsync(request, ct);
    }

    private bool IsTranslationUnnecessary(TranslationRequest request)
    {
        bool isTextEmpty = string.IsNullOrWhiteSpace(request.Text);
        bool isSameLanguage = (request.From != Language.Auto) && (request.From == request.To);
        
        return isTextEmpty || isSameLanguage;
    }

    private TranslationResult ReturnOriginalText(TranslationRequest request)
    {
        string safeText = request.Text ?? string.Empty;
        return new TranslationResult(safeText, request.From, request.To);
    }

    private async Task<TranslationResult> PerformTranslationAsync(TranslationRequest request, CancellationToken ct)
    {
        string text = request.Text;

        // إذا كان النص قصيراً (أقل من أو يساوي الحد الأقصى للمقطع)، يترجم مباشرة دفعة واحدة
        if (text.Length <= SmartTextSplitter.DefaultMaxChunkSize)
        {
            return await _translator.TranslateAsync(request, ct);
        }

        // تقسيم النص الطويل إلى مقاطع سياقية ذكية تحترم حدود الجمل والفقرات
        var chunks = SmartTextSplitter.Split(text, SmartTextSplitter.DefaultMaxChunkSize);

        if (chunks.Count <= 1)
        {
            return await _translator.TranslateAsync(request, ct);
        }

        var combinedResult = new StringBuilder();
        Language? detectedLanguage = null;
        string? detectedLanguageName = null;

        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                combinedResult.Append(chunk.Content);
                combinedResult.Append(chunk.Suffix);
                continue;
            }

            var chunkRequest = new TranslationRequest(chunk.Content, request.From, request.To);
            var translatedChunk = await _translator.TranslateAsync(chunkRequest, ct);

            if (detectedLanguage == null && translatedChunk.DetectedLanguage.HasValue)
            {
                detectedLanguage = translatedChunk.DetectedLanguage;
                detectedLanguageName = translatedChunk.DetectedLanguageName;
            }

            combinedResult.Append(translatedChunk.Text);
            combinedResult.Append(chunk.Suffix);
        }

        return new TranslationResult(combinedResult.ToString(), request.From, request.To, detectedLanguage, detectedLanguageName);
    }
}