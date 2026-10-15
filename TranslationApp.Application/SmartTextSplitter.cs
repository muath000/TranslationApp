using System;
using System.Collections.Generic;
using System.Text;

namespace TranslationApp.Application;

/// <summary>
/// يمثل مقطعاً نصياً مجتزءاً مع اللاحقة الفاصلة التابعة له (مثل سطرين جديدين، سطر، أو مسافة)
/// </summary>
public sealed record TextChunk(string Content, string Suffix);

/// <summary>
/// مقسم نصوص ذكي يقسم النصوص الطويلة إلى مقاطع سياقية متوازنة خلف الكواليس
/// مع الحفاظ التام والرياضي 100% على بنية الفقرات والأسطر والفواصل
/// </summary>
public static class SmartTextSplitter
{
    /// <summary>
    /// الحد الأقصى الآمن لحجم المقطع الواحد (380 حرف لضمان عدم تجاوز قيود أي محرك مثل MyMemory بحد 500 حرف)
    /// </summary>
    public const int DefaultMaxChunkSize = 380;

    /// <summary>
    /// يقسم النص إلى مقاطع سياقية ذكية تحترم حدود الفقرات والجمل والكلمات
    /// </summary>
    public static List<TextChunk> Split(string text, int maxChunkSize = DefaultMaxChunkSize)
    {
        var result = new List<TextChunk>();

        if (string.IsNullOrEmpty(text))
            return result;

        if (text.Length <= maxChunkSize)
        {
            result.Add(new TextChunk(text, string.Empty));
            return result;
        }

        // 1. التقسيم أولاً بحسب الفقرات (أسطر جديدة مزدوجة ومفردة)
        var rawParagraphs = SplitByDelimiters(text, new[] { "\r\n\r\n", "\n\n", "\r\r", "\r\n", "\n" });

        var currentContent = new StringBuilder();
        var currentSuffix = string.Empty;

        foreach (var (content, delimiter) in rawParagraphs)
        {
            // إذا كانت هذه الفقرة بحد ذاتها أطول من الحد الأقصى، نقسمها حسب الجمل
            if (content.Length > maxChunkSize)
            {
                if (currentContent.Length > 0)
                {
                    result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                    currentContent.Clear();
                    currentSuffix = string.Empty;
                }

                var sentenceChunks = SplitLongParagraphIntoSentences(content, delimiter, maxChunkSize);
                result.AddRange(sentenceChunks);
            }
            else
            {
                int projectedLength = currentContent.Length + (currentContent.Length > 0 ? currentSuffix.Length : 0) + content.Length;
                if (projectedLength <= maxChunkSize)
                {
                    if (currentContent.Length > 0)
                    {
                        currentContent.Append(currentSuffix);
                    }
                    currentContent.Append(content);
                    currentSuffix = delimiter;
                }
                else
                {
                    if (currentContent.Length > 0)
                    {
                        result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                        currentContent.Clear();
                    }

                    currentContent.Append(content);
                    currentSuffix = delimiter;
                }
            }
        }

        if (currentContent.Length > 0)
        {
            result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
        }

        return result;
    }

    private static List<TextChunk> SplitLongParagraphIntoSentences(string paragraph, string finalDelimiter, int maxChunkSize)
    {
        var result = new List<TextChunk>();

        // تقسيم بحسب نهايات الجمل وعلامات الترقيم الأساسية
        var sentenceDelimiters = new[] { ". ", "? ", "! ", "؟ ", "!\n", ".\n", "?\n", "؛ ", "; " };
        var rawSentences = SplitByDelimiters(paragraph, sentenceDelimiters);

        var currentContent = new StringBuilder();
        var currentSuffix = string.Empty;

        for (int i = 0; i < rawSentences.Count; i++)
        {
            var (content, delimiter) = rawSentences[i];
            bool isLast = (i == rawSentences.Count - 1);
            string effectiveDelimiter = isLast ? finalDelimiter : delimiter;

            if (content.Length > maxChunkSize)
            {
                if (currentContent.Length > 0)
                {
                    result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                    currentContent.Clear();
                    currentSuffix = string.Empty;
                }

                // تقسيم الجملة الطويلة جداً بحسب الفواصل والمسافات
                var clauseChunks = SplitLongSentenceByWords(content, effectiveDelimiter, maxChunkSize);
                result.AddRange(clauseChunks);
            }
            else
            {
                int projectedLength = currentContent.Length + (currentContent.Length > 0 ? currentSuffix.Length : 0) + content.Length;
                if (projectedLength <= maxChunkSize)
                {
                    if (currentContent.Length > 0)
                    {
                        currentContent.Append(currentSuffix);
                    }
                    currentContent.Append(content);
                    currentSuffix = effectiveDelimiter;
                }
                else
                {
                    if (currentContent.Length > 0)
                    {
                        result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                        currentContent.Clear();
                    }

                    currentContent.Append(content);
                    currentSuffix = effectiveDelimiter;
                }
            }
        }

        if (currentContent.Length > 0)
        {
            result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
        }

        return result;
    }

    private static List<TextChunk> SplitLongSentenceByWords(string sentence, string finalDelimiter, int maxChunkSize)
    {
        var result = new List<TextChunk>();

        // تقسيم بالكلمات والفواصل
        var words = SplitByDelimiters(sentence, new[] { ", ", "، ", " " });

        var currentContent = new StringBuilder();
        var currentSuffix = string.Empty;

        for (int i = 0; i < words.Count; i++)
        {
            var (content, delimiter) = words[i];
            bool isLast = (i == words.Count - 1);
            string effectiveDelimiter = isLast ? finalDelimiter : delimiter;

            if (content.Length > maxChunkSize)
            {
                // كلمة واحدة أطول من الحد (مثل نص عشوائي طويل أو رابط)، نقسمها قسرياً
                if (currentContent.Length > 0)
                {
                    result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                    currentContent.Clear();
                    currentSuffix = string.Empty;
                }

                for (int pos = 0; pos < content.Length; pos += maxChunkSize)
                {
                    int len = Math.Min(maxChunkSize, content.Length - pos);
                    string part = content.Substring(pos, len);
                    bool isVeryLastPart = (pos + len >= content.Length);
                    result.Add(new TextChunk(part, isVeryLastPart ? effectiveDelimiter : string.Empty));
                }
            }
            else
            {
                int projectedLength = currentContent.Length + (currentContent.Length > 0 ? currentSuffix.Length : 0) + content.Length;
                if (projectedLength <= maxChunkSize)
                {
                    if (currentContent.Length > 0)
                    {
                        currentContent.Append(currentSuffix);
                    }
                    currentContent.Append(content);
                    currentSuffix = effectiveDelimiter;
                }
                else
                {
                    if (currentContent.Length > 0)
                    {
                        result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
                        currentContent.Clear();
                    }

                    currentContent.Append(content);
                    currentSuffix = effectiveDelimiter;
                }
            }
        }

        if (currentContent.Length > 0)
        {
            result.Add(new TextChunk(currentContent.ToString(), currentSuffix));
        }

        return result;
    }

    private static List<(string Content, string Delimiter)> SplitByDelimiters(string text, string[] delimiters)
    {
        var result = new List<(string Content, string Delimiter)>();
        int startIndex = 0;

        while (startIndex < text.Length)
        {
            int earliestIndex = -1;
            string? matchedDelimiter = null;

            foreach (var delim in delimiters)
            {
                int idx = text.IndexOf(delim, startIndex, StringComparison.Ordinal);
                if (idx != -1 && (earliestIndex == -1 || idx < earliestIndex))
                {
                    earliestIndex = idx;
                    matchedDelimiter = delim;
                }
            }

            if (earliestIndex != -1 && matchedDelimiter != null)
            {
                string content = text.Substring(startIndex, earliestIndex - startIndex);
                result.Add((content, matchedDelimiter));
                startIndex = earliestIndex + matchedDelimiter.Length;
            }
            else
            {
                string content = text.Substring(startIndex);
                result.Add((content, string.Empty));
                break;
            }
        }

        return result;
    }
}
