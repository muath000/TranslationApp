using System;

namespace TranslationApp.Domain;

public enum Language
{
    Auto = 0,
    Arabic = 1,
    English = 2,
    French = 3,
    German = 4,
    Spanish = 5,
    Turkish = 6
}

/// <summary>
/// قناع بتات لحزم اللغات المدعومة
/// </summary>
[Flags]
public enum LanguageBitmask : ulong
{
    None    = 0,
    Arabic  = 1UL << 0,
    English = 1UL << 1,
    French  = 1UL << 2,
    German  = 1UL << 3,
    Spanish = 1UL << 4,
    Turkish = 1UL << 5
}

public static class LanguageBitmaskExtensions
{
    public static LanguageBitmask ToBitmask(this Language lang) => lang switch
    {
        Language.Arabic => LanguageBitmask.Arabic,
        Language.English => LanguageBitmask.English,
        Language.French => LanguageBitmask.French,
        Language.German => LanguageBitmask.German,
        Language.Spanish => LanguageBitmask.Spanish,
        Language.Turkish => LanguageBitmask.Turkish,
        _ => LanguageBitmask.None
    };

    public static string GetDisplayName(this Language lang, bool isArabicUi = true) => lang switch
    {
        Language.Auto => isArabicUi ? "اكتشاف تلقائي" : "Auto Detect",
        Language.Arabic => "العربية",
        Language.English => "English",
        Language.French => "Français",
        Language.German => "Deutsch",
        Language.Spanish => "Español",
        Language.Turkish => "Türkçe",
        _ => lang.ToString()
    };
}

/// <summary>
/// يمثل زوج لغتي المصدر والهدف ويدير قواعد التبديل والتعديل التلقائي
/// </summary>
public readonly record struct LanguagePair(Language Source, Language Target)
{
    public static LanguagePair Default => new(Language.Arabic, Language.English);

    /// <summary>
    /// تبديل اللغتين مع التعامل السليم مع حالة الاكتشاف التلقائي
    /// </summary>
    public LanguagePair Swap(Language? detectedLanguage = null)
    {
        if (Source == Language.Auto)
        {
            if (detectedLanguage.HasValue && detectedLanguage.Value != Language.Auto)
            {
                return new LanguagePair(Target, detectedLanguage.Value);
            }

            Language fallbackTarget = (Target == Language.Arabic) ? Language.English : Language.Arabic;
            return new LanguagePair(Target, fallbackTarget);
        }

        return new LanguagePair(Target, Source);
    }

    /// <summary>
    /// ضبط لغة المصدر وضبط لغة الهدف تلقائياً إذا تماثلتا
    /// </summary>
    public LanguagePair WithSource(Language newSource)
    {
        if (newSource != Language.Auto && newSource == Target)
        {
            Language adjustedTarget = (newSource == Language.Arabic) ? Language.English : Language.Arabic;
            return new LanguagePair(newSource, adjustedTarget);
        }

        return new LanguagePair(newSource, Target);
    }

    /// <summary>
    /// ضبط لغة الهدف وضبط لغة المصدر تلقائياً إذا تماثلتا
    /// </summary>
    public LanguagePair WithTarget(Language newTarget)
    {
        if (newTarget == Source)
        {
            return new LanguagePair(Language.Auto, newTarget);
        }

        return new LanguagePair(Source, newTarget);
    }

    /// <summary>
    /// صياغة الاسم المعروض للغة المصدر مع إبراز اللغة المكتشفة
    /// </summary>
    public string FormatSourceDisplayName(bool isArabicUi, Language? detectedLanguage = null)
    {
        if (Source == Language.Auto)
        {
            if (detectedLanguage.HasValue && detectedLanguage.Value != Language.Auto)
            {
                string detectedName = detectedLanguage.Value.GetDisplayName(isArabicUi);
                return isArabicUi 
                    ? $"اكتشاف تلقائي ({detectedName})" 
                    : $"Auto Detect ({detectedName})";
            }

            return isArabicUi ? "اكتشاف تلقائي" : "Auto Detect";
        }

        return Source.GetDisplayName(isArabicUi);
    }

    /// <summary>
    /// صياغة الاسم المعروض للغة الهدف
    /// </summary>
    public string FormatTargetDisplayName(bool isArabicUi)
    {
        return Target.GetDisplayName(isArabicUi);
    }
}
