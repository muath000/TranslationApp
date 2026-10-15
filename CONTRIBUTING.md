# المساهمة في المشروع / Contributing to TranslationApp

[English version below](#contributing-guidelines-english)

---

## إرشادات المساهمة (العربية)

نشكرك على اهتمامك بالمساهمة في تطوير **TranslationApp**! نسعى لبناء تطبيق ترجمة فوري عصري ومثالي لنظام Windows 11، ونرحب بكافة المساهمات من مجتمع المطورين.

### 1. المتطلبات الأساسية
- **نظام التشغيل:** Windows 10 الإصدار 19041 فأحدث أو Windows 11.
- **حزمة التطوير:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (أو أحدث).
- **بيئة التطوير:** Visual Studio 2022 (v17.12+) أو Visual Studio Code مع C# Dev Kit.

### 2. البناء والتشغيل محلياً
```pwsh
# استنساخ المستودع
git clone https://github.com/muath000/TranslationApp.git
cd TranslationApp

# استعادة الحزم والبناء
dotnet build -c Release

# تشغيل الاختبارات الآلية (يجب اجتياز كافة الاختبارات)
dotnet test -c Release

# إنتاج الملف التنفيذي المستقل (Single-File Exe)
dotnet publish TranslationApp.UI\TranslationApp.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\Publish\v1.0.0
```

### 3. المعمارية ودستور التطوير
يرتكز المشروع على معمارية **Clean Architecture** ومبادئ مشددة موضحة في [`AGENTS.md`](./AGENTS.md). يرجى الالتزام بالمعايير التالية:
- **فصل المسؤوليات:** الحفاظ على استقلالية الطبقات (`Domain` -> `Application` -> `Infrastructure` / `UI`).
- **نظافة الكود:** تجنب كتابة تعليقات بديهية أو كود غير مستخدم، والاعتماد على تسميات واضحة توثق نفسها.
- **إدارة الموارد:** استخدام `async / await` السليم، ودعم `CancellationToken`، وتفادي تسريبات الذاكرة والأحداث.
- **هوية Windows 11 الأصلية:** احترام الخطوط والأيقونات الرسمية والسمات (Light / Dark).

### 4. اتفاقية رسائل الالتزام (Commit Convention)
نستخدم نمط **Conventional Commits**:
- `feat: إضافة ميزة جديدة`
- `fix: معالجة خطأ برمجي`
- `docs: تحديث التوثيق`
- `refactor: إعادة تنظيم كود دون تغيير السلوك الخارجي`
- `test: إضافة أو تعديل اختبارات`
- `chore: مهام صيانة وإعدادات`

### 5. خطوات تقديم طلبات السحب (Pull Requests)
1. قم بإنشاء فرع منفصل للميزة أو الإصلاح: `git checkout -b feature/your-feature-name`.
2. تأكد من اجتياز جميع الاختبارات المحلية: `dotnet test -c Release`.
3. التزم بعدم تعديل أي ملفات لا تتعلق بطلب السحب الخاص بك.
4. افتح Pull Request موضحاً التغييرات بوضوح، واربطه برقم المشكلة (Issue) المتعلقة إن وجدت.

### 6. نظام ترقيم الإصدارات (Semantic Versioning)
يتبع المشروع معيار `MAJOR.MINOR.PATCH`:
- **PATCH** (مثل `1.0.1`): لإصلاح الأخطاء الطفيفة والتحسينات السريعة.
- **MINOR** (مثل `1.1.0`): لإضافة ميزات جديدة متوافقة مع الإصدارات السابقة.
- **MAJOR** (مثل `2.0.0`): للتغييرات المعمارية الجذرية أو غير المتوافقة.

---

<a name="contributing-guidelines-english"></a>
## Contributing Guidelines (English)

Thank you for your interest in contributing to **TranslationApp**! We welcome bug fixes, improvements, and feature contributions from the community.

### 1. Prerequisites
- **OS:** Windows 10 (Build 19041+) or Windows 11.
- **SDK:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.
- **IDE:** Visual Studio 2022 (17.12+) or Visual Studio Code with the C# Dev Kit.

### 2. Building & Testing Locally
```pwsh
# Clone repository
git clone https://github.com/muath000/TranslationApp.git
cd TranslationApp

# Build solution
dotnet build -c Release

# Run automated tests (all tests must pass)
dotnet test -c Release

# Publish standalone single-file executable
dotnet publish TranslationApp.UI\TranslationApp.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\Publish\v1.0.0
```

### 3. Architecture & Guidelines
This project follows **Clean Architecture** and strict architectural rules detailed in [`AGENTS.md`](./AGENTS.md):
- **Layer Separation:** Respect dependency flow (`Domain` -> `Application` -> `Infrastructure` / `UI`).
- **Clean Code:** Self-documenting code with clear naming. Avoid obvious/redundant comments.
- **Async & Performance:** Proper `async / await`, non-blocking UI thread, `CancellationToken` support, and disciplined resource disposal.
- **Native Windows 11 Experience:** Maintain native Fluent design, system theme adaptations, and accessibility.

### 4. Conventional Commits
Please follow the standard commit convention:
- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `refactor:` Code refactoring without behavior changes
- `test:` Adding or updating tests
- `chore:` Maintenance, builds, or dependencies

### 5. Pull Request Process
1. Create a descriptive feature branch: `git checkout -b feature/your-feature-name`.
2. Ensure tests pass locally: `dotnet test -c Release`.
3. Keep changes atomic and directly relevant to the issue/feature.
4. Submit the Pull Request with a clear description of changes.

### 6. Semantic Versioning
This project follows `MAJOR.MINOR.PATCH`:
- **PATCH** (e.g. `1.0.1`): Bug fixes and minor maintenance.
- **MINOR** (e.g. `1.1.0`): Backward-compatible new features.
- **MAJOR** (e.g. `2.0.0`): Breaking architectural or functional changes.
