# TranslationApp

[English](#english) | [العربية](#العربية)

---

## English

A fast, lightweight Windows translation app with a native Windows 11 look. Translate between Arabic and English (and more languages) in a clean, modern window.

### Features

- **Native Windows 11 design**: Fluent UI with Mica background, rounded corners, Segoe UI Variable.
- **Reliable translation**: automatic fallback across three translation sources, so the app keeps working if one fails.
- **Long texts**: smart splitting for texts up to 5000 characters, respecting paragraphs and sentences.
- **Automatic text direction**: instant RTL/LTR detection for Arabic and English.
- **Light and dark themes**: manual, or follow the system.
- **Arabic and English interface**.
- **Always on top** pin, and **auto-copy** of the result to the clipboard.
- **Low memory usage**.

### Download and run

1. Go to [Releases](https://github.com/muath000/TranslationApp/releases) and download `TranslationApp.exe`.
2. Double-click it. No installation, no .NET needed.

> Windows may show a SmartScreen warning because the file is not code-signed. Click **More info → Run anyway**.

**Requirements:** Windows 10 or 11 (64-bit), internet connection.

> Translation relies on free third-party online services, which may change or have usage limits.

### Build from source

Requires the .NET 9 SDK.

```powershell
dotnet publish TranslationApp.UI\TranslationApp.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### Versioning

Semantic Versioning `MAJOR.MINOR.PATCH`: bug fix → `1.0.1`, new feature → `1.1.0`, major change → `2.0.0`.

---

## العربية

<div dir="rtl">

تطبيق ترجمة سريع وخفيف لنظام Windows بتصميم Windows 11 الأصلي. يترجم بين العربية والإنجليزية (ولغات أخرى) في نافذة نظيفة وعصرية.

### المميزات

- **تصميم Windows 11 الأصلي**: واجهة Fluent مع خلفية Mica وحواف دائرية وخط Segoe UI Variable.
- **ترجمة موثوقة**: تبديل تلقائي بين ثلاثة مصادر للترجمة، فيستمر التطبيق بالعمل إذا تعطل أحدها.
- **النصوص الطويلة**: تقسيم ذكي للنصوص حتى 5000 حرف مع احترام الفقرات والجمل.
- **اتجاه النص تلقائيًا**: كشف فوري لاتجاه العربية والإنجليزية (RTL/LTR).
- **ثيم فاتح وداكن**: يدويًا أو حسب النظام.
- **واجهة بالعربية والإنجليزية**.
- **تثبيت النافذة فوق النوافذ** و**نسخ النتيجة تلقائيًا** إلى الحافظة.
- **استهلاك منخفض للذاكرة**.

### التحميل والتشغيل

1. ادخل على [Releases](https://github.com/muath000/TranslationApp/releases) وحمّل `TranslationApp.exe`.
2. شغّله بنقرتين. لا يحتاج تثبيت ولا .NET.

> قد يظهر تحذير SmartScreen لأن الملف غير موقّع رقميًا. اضغط **More info ← Run anyway**.

**المتطلبات:** Windows 10 أو 11 (64-bit)، واتصال بالإنترنت.

> الترجمة تعتمد على خدمات مجانية خارجية، وقد تتغير أو يكون لها حدود استخدام.

### البناء من المصدر

يتطلب .NET 9 SDK.

```powershell
dotnet publish TranslationApp.UI\TranslationApp.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### الإصدارات

الترقيم `MAJOR.MINOR.PATCH`: إصلاح خطأ ← `1.0.1`، ميزة جديدة ← `1.1.0`، تغيير كبير ← `2.0.0`.

</div>