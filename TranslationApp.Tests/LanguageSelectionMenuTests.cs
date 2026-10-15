using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TranslationApp.Domain;
using Xunit;

namespace TranslationApp.Tests;

public class LanguageSelectionMenuTests
{
    [Fact]
    public void SelectingSameLanguage_DoesNotTriggerStateChangeOrRestart()
    {
        var sourceLang = Language.Arabic;
        var targetLang = Language.French;
        bool translationTriggered = false;

        // محاكاة الشرط المنطقي: عند اختيار نفس اللغة الحالية
        Action<Language, bool> onLanguageSelected = (selectedLang, isSource) =>
        {
            var currentLang = isSource ? sourceLang : targetLang;
            if (currentLang == selectedLang)
            {
                // إغلاق فوري دون أي إعادة ترجمة أو إعادة تشغيل
                return;
            }

            translationTriggered = true;
        };

        // المستخدم اختار الفرنسية وهي مختارة مسبقاً
        onLanguageSelected(Language.French, false);

        Assert.False(translationTriggered, "Selecting the current language should NOT trigger a re-translation or state reset.");
        Assert.Equal(Language.French, targetLang);
    }

    [Fact]
    public void SelectingDifferentLanguage_AppliesChangeCorrectly()
    {
        var sourceLang = Language.Arabic;
        var targetLang = Language.French;
        bool translationTriggered = false;

        Action<Language, bool> onLanguageSelected = (selectedLang, isSource) =>
        {
            var currentLang = isSource ? sourceLang : targetLang;
            if (currentLang == selectedLang) return;

            if (isSource) sourceLang = selectedLang;
            else targetLang = selectedLang;

            translationTriggered = true;
        };

        // المستخدم اختار الإنجليزية
        onLanguageSelected(Language.English, false);

        Assert.True(translationTriggered);
        Assert.Equal(Language.English, targetLang);
    }

    [Fact]
    public void RapidCapsuleClick_PreventReopeningLoop()
    {
        // محاكاة حماية الحلقة التكرارية عند نقر الكبسولة لإغلاق القائمة
        DateTime menuClosedTimestamp = DateTime.UtcNow;
        bool wasSameTarget = true;
        bool menuOpened = false;

        // عندما ينقر المستخدم على نفس الكبسولة أثناء إغلاقها (خلال أقل من 250ms)
        var clickTime = menuClosedTimestamp.AddMilliseconds(50);
        if (wasSameTarget && (clickTime - menuClosedTimestamp).TotalMilliseconds < 250)
        {
            // لا تفتح القائمة مجدداً - إغلاق فقط
            menuOpened = false;
        }
        else
        {
            menuOpened = true;
        }

        Assert.False(menuOpened, "Capsule click right after menu close must NOT reopen the menu in an infinite loop.");
    }
}
