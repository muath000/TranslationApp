using System;

namespace TranslationApp.Domain;

/// <summary>
/// أسباب فشل عملية الترجمة لتحديد رسالة الخطأ الدقيقة للمستخدم
/// </summary>
public enum TranslationFailureReason
{
    NetworkUnavailable,
    Timeout,
    ServiceUnavailable,
    TextTooLong
}

/// <summary>
/// استثناء موحد لأخطاء خدمة الترجمة
/// </summary>
public class TranslationException : Exception
{
    public TranslationFailureReason Reason { get; }

    public TranslationException(TranslationFailureReason reason, string message, Exception? innerException = null) 
        : base(message, innerException)
    {
        Reason = reason;
    }
}

/// <summary>
/// نموذج معلومات الخطأ المعروض للمستخدم
/// </summary>
public readonly record struct TranslationErrorInfo(string Icon, string Title, string Message);

public static class TranslationFailureExtensions
{
    public static TranslationErrorInfo GetDisplayInfo(this TranslationFailureReason reason, bool isArabicUi) => reason switch
    {
        TranslationFailureReason.NetworkUnavailable => new(
            "\uE783",
            isArabicUi ? "انقطاع الاتصال بالإنترنت" : "No Internet Connection",
            isArabicUi 
                ? "تعذر الاتصال بخوادم الترجمة. يرجى التحقق من الشبكة وإعادة المحاولة." 
                : "Could not reach translation servers. Please check your network and try again."),

        TranslationFailureReason.Timeout => new(
            "\uE823",
            isArabicUi ? "انتهاء مهلة الطلب" : "Request Timed Out",
            isArabicUi 
                ? "استغرقت عملية الترجمة وقتاً أطول من المعتاد. يرجى إعادة المحاولة." 
                : "The translation request took too long. Please try again."),

        TranslationFailureReason.TextTooLong => new(
            "\uE7BA",
            isArabicUi ? "تجاوز النص الحد الأقصى" : "Text Exceeds Limit",
            isArabicUi 
                ? "الحد الأقصى المسموح به للترجمة هو 5000 حرف." 
                : "Maximum allowed text limit is 5000 characters."),

        _ => new(
            "\uE7BA",
            isArabicUi ? "خدمة الترجمة غير متاحة حالياً" : "Service Temporarily Unavailable",
            isArabicUi 
                ? "تعذر الحصول على الترجمة من جميع الخوادم. يرجى المحاولة بعد قليل." 
                : "Could not retrieve translation from servers. Please try again shortly.")
    };
}
