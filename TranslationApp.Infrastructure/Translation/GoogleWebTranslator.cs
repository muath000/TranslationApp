using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using TranslationApp.Application;
using TranslationApp.Domain;

namespace TranslationApp.Infrastructure.Translation;

/// <summary>
/// محرك ترجمة فائق الاعتمادية والواقعية (Resilient Multi-Engine Translator).
/// يدعم الاكتشاف التلقائي للغة المصدر ويعمل بنظام تعافي متعدد الطبقات:
/// 1. المستوى الأول: Google Neural API عبر HttpClient مجهز برؤوس متصفح ودعم POST/GET.
/// 2. المستوى الثاني: Windows curl Bridge المدمج في النظام لنفس الرابط مع تمرير آمن للمعاملات.
/// 3. المستوى الثالث: MyMemory API العصبي المستقل كبديل مجاني موثوق ومفصل للترجمة الواقعية.
/// </summary>
public class GoogleWebTranslator : ITranslator
{
    private static readonly HttpClient _httpClient;

    private sealed record TranslationPayload(string Text, Language? DetectedLanguage, string? DetectedLanguageName);

    static GoogleWebTranslator()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            AutomaticDecompression = DecompressionMethods.All
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AndroidTranslate/5.3.0.RC02.130475354-53000263 5.1 phone TRANSLATE_OPM5_TEST_1");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,ar;q=0.8");
    }

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.To == Language.Auto)
        {
            throw new ArgumentException("لا يمكن اختيار الاكتشاف التلقائي كلغة هدف.", nameof(request));
        }

        string sourceLang = GetLanguageCode(request.From);
        string targetLang = GetLanguageCode(request.To);

        bool networkFailed = false;
        bool timeoutOccurred = false;

        // 1. المحاولة الأساسية: Google API عبر HttpClient
        try
        {
            var payload = await TryGoogleHttpAsync(request.Text, sourceLang, targetLang, ct);
            if (payload != null && !string.IsNullOrWhiteSpace(payload.Text))
            {
                return new TranslationResult(payload.Text, request.From, request.To, payload.DetectedLanguage, payload.DetectedLanguageName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            networkFailed = true;
        }
        catch (TimeoutException)
        {
            timeoutOccurred = true;
        }
        catch { }

        // 2. المحاولة الاحتياطية الأولى: Windows curl Bridge لنفس خادم Google (تجاوز بصمة TLS)
        try
        {
            var payload = await TryGoogleCurlAsync(request.Text, sourceLang, targetLang, ct);
            if (payload != null && !string.IsNullOrWhiteSpace(payload.Text))
            {
                return new TranslationResult(payload.Text, request.From, request.To, payload.DetectedLanguage, payload.DetectedLanguageName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch { }

        // 3. المحاولة الاحتياطية الثانية: محرك MyMemory العصبي المستقل
        try
        {
            var payload = await TryMyMemoryHttpAsync(request.Text, sourceLang, targetLang, ct);
            if (payload != null && !string.IsNullOrWhiteSpace(payload.Text))
            {
                return new TranslationResult(payload.Text, request.From, request.To, payload.DetectedLanguage, payload.DetectedLanguageName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            networkFailed = true;
        }
        catch (TimeoutException)
        {
            timeoutOccurred = true;
        }
        catch { }

        if (!NetworkInterface.GetIsNetworkAvailable() || networkFailed)
        {
            throw new TranslationException(TranslationFailureReason.NetworkUnavailable, "تعذر الاتصال بخوادم الترجمة. يرجى التحقق من اتصال الإنترنت وإعادة المحاولة.");
        }

        if (timeoutOccurred)
        {
            throw new TranslationException(TranslationFailureReason.Timeout, "انتهت مهلة الطلب. استغرقت خوادم الترجمة وقتاً أطول من المتوقع، يرجى إعادة المحاولة.");
        }

        throw new TranslationException(TranslationFailureReason.ServiceUnavailable, "خوادم الترجمة غير متاحة حالياً. يرجى المحاولة بعد لحظات.");
    }

    private static async Task<TranslationPayload?> TryGoogleHttpAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        string[] baseUrls = new[]
        {
            $"https://translate.google.com/translate_a/single?client=at&sl={sourceLang}&tl={targetLang}&dt=t",
            $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t"
        };

        foreach (var baseUrl in baseUrls)
        {
            if (ct.IsCancellationRequested) break;

            // المحاولة عبر POST لتجنب حدود طول الرابط والحصول على استجابة سريعة
            try
            {
                using var postContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", text)
                });

                using var postResponse = await _httpClient.PostAsync(baseUrl, postContent, ct);
                if (postResponse.IsSuccessStatusCode)
                {
                    string json = await postResponse.Content.ReadAsStringAsync(ct);
                    var parsed = ParseGoogleJsonResponse(json, text);
                    if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Text))
                    {
                        return parsed;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { }

            // محاولة GET للنصوص القصيرة (أقل من 600 حرف)
            if (text.Length < 600)
            {
                try
                {
                    string encodedText = Uri.EscapeDataString(text);
                    string getUrl = $"{baseUrl}&q={encodedText}";

                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, getUrl);
                    using var response = await _httpClient.SendAsync(requestMessage, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync(ct);
                        var parsed = ParseGoogleJsonResponse(json, text);
                        if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Text))
                        {
                            return parsed;
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch { }
            }
        }

        return null;
    }

    private static async Task<TranslationPayload?> TryGoogleCurlAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        string[] urls = new[]
        {
            $"https://translate.google.com/translate_a/single?client=at&sl={sourceLang}&tl={targetLang}&dt=t",
            $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t"
        };

        foreach (var url in urls)
        {
            if (ct.IsCancellationRequested) break;

            var psi = new ProcessStartInfo
            {
                FileName = "curl.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            psi.ArgumentList.Add("-s");
            psi.ArgumentList.Add("--max-time");
            psi.ArgumentList.Add("5");
            psi.ArgumentList.Add("-A");
            psi.ArgumentList.Add("AndroidTranslate/5.3.0.RC02.130475354-53000263 5.1 phone TRANSLATE_OPM5_TEST_1");
            psi.ArgumentList.Add("-X");
            psi.ArgumentList.Add("POST");
            psi.ArgumentList.Add("--data-urlencode");
            psi.ArgumentList.Add($"q={text}");
            psi.ArgumentList.Add(url);

            using var process = new Process { StartInfo = psi };
            try
            {
                if (!process.Start())
                {
                    continue;
                }

                var outputTask = process.StandardOutput.ReadToEndAsync(ct);
                var waitTask = process.WaitForExitAsync(ct);

                var completedTask = await Task.WhenAny(waitTask, Task.Delay(5000, ct));

                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    continue;
                }

                string output = await outputTask;
                if (string.IsNullOrWhiteSpace(output) || process.ExitCode != 0)
                {
                    continue;
                }

                var parsed = ParseGoogleJsonResponse(output, text);
                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Text))
                {
                    return parsed;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                throw;
            }
            catch
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            }
        }

        return null;
    }

    private static async Task<TranslationPayload?> TryMyMemoryHttpAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        try
        {
            string myMemorySource = (sourceLang == "auto") ? "autodetect" : sourceLang;
            string encodedText = Uri.EscapeDataString(text);
            string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={myMemorySource}|{targetLang}&de=translationapp@outlook.com";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(requestMessage, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("responseStatus", out var statusElem))
            {
                if (statusElem.ValueKind == JsonValueKind.Number && statusElem.GetInt32() != 200)
                {
                    return null;
                }
            }

            if (root.TryGetProperty("responseData", out var responseData) &&
                responseData.TryGetProperty("translatedText", out var translatedTextElem))
            {
                string? translated = translatedTextElem.GetString();
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    // استبعاد رسائل الخطأ أو التحذيرات الخاصة بـ MyMemory
                    if (translated.Contains("QUERY LENGTH LIMIT EXCEEDED", StringComparison.OrdinalIgnoreCase) ||
                        translated.Contains("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase) ||
                        translated.Contains("NO QUERY SPECIFIED", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    string resultText = WebUtility.HtmlDecode(translated.Trim());

                    Language? detectedLanguage = null;
                    string? detectedLanguageName = null;

                    bool hasLetters = text.Any(char.IsLetter);
                    if (hasLetters && responseData.TryGetProperty("detectedLanguage", out var detectedElem))
                    {
                        string? detectedCode = detectedElem.GetString();
                        if (!string.IsNullOrWhiteSpace(detectedCode))
                        {
                            detectedLanguage = MapLanguageCode(detectedCode);
                            detectedLanguageName = detectedLanguage?.GetDisplayName() ?? ResolveLanguageName(detectedCode);
                        }
                    }

                    return new TranslationPayload(resultText, detectedLanguage, detectedLanguageName);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static TranslationPayload? ParseGoogleJsonResponse(string jsonResponse, string originalText)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var rootArray = doc.RootElement;

            if (rootArray.ValueKind == JsonValueKind.Array && rootArray.GetArrayLength() > 0)
            {
                var translationsArray = rootArray[0];
                if (translationsArray.ValueKind != JsonValueKind.Array)
                    return null;

                var fullTranslation = new StringBuilder();

                foreach (var chunk in translationsArray.EnumerateArray())
                {
                    if (chunk.ValueKind == JsonValueKind.Array && chunk.GetArrayLength() > 0)
                    {
                        string translatedChunk = chunk[0].GetString() ?? string.Empty;
                        fullTranslation.Append(translatedChunk);
                    }
                }

                string resultText = fullTranslation.ToString().Trim();
                if (string.IsNullOrEmpty(resultText))
                    return null;

                Language? detectedLanguage = null;
                string? detectedLanguageName = null;

                // التحقق من موثوقية اكتشاف اللغة:
                // لا يدّعي اكتشاف اللغة إذا كان النص لا يحتوي على أي أحرف (أرقام ورموز فقط)
                bool hasLetters = originalText.Any(char.IsLetter);

                if (hasLetters && rootArray.GetArrayLength() > 2)
                {
                    double confidence = 1.0;
                    if (rootArray.GetArrayLength() > 6 && rootArray[6].ValueKind == JsonValueKind.Number)
                    {
                        confidence = rootArray[6].GetDouble();
                    }

                    // تجاهل الاكتشاف إذا كانت نسبة الثقة 0 أو معدومة
                    if (confidence > 0.05)
                    {
                        string? langCode = rootArray[2].GetString();
                        if (!string.IsNullOrWhiteSpace(langCode))
                        {
                            detectedLanguage = MapLanguageCode(langCode);
                            detectedLanguageName = detectedLanguage?.GetDisplayName() ?? ResolveLanguageName(langCode);
                        }
                    }
                }

                return new TranslationPayload(resultText, detectedLanguage, detectedLanguageName);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static Language? MapLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        string primary = code.Split('-')[0].ToLowerInvariant().Trim();
        return primary switch
        {
            "ar" => Language.Arabic,
            "en" => Language.English,
            "fr" => Language.French,
            "de" => Language.German,
            "es" => Language.Spanish,
            "tr" => Language.Turkish,
            _ => null
        };
    }

    private static string? ResolveLanguageName(string code)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(code);
            return culture.NativeName;
        }
        catch
        {
            return code;
        }
    }

    private static string GetLanguageCode(Language lang)
    {
        return lang switch
        {
            Language.Auto => "auto",
            Language.English => "en",
            Language.Arabic => "ar",
            Language.French => "fr",
            Language.German => "de",
            Language.Spanish => "es",
            Language.Turkish => "tr",
            _ => throw new NotSupportedException($"Language {lang} is not supported")
        };
    }
}
