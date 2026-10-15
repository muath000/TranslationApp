namespace TranslationApp.Domain;

public sealed record TranslationRequest(string Text, Language From, Language To);

public sealed record TranslationResult(
    string Text, 
    Language From, 
    Language To, 
    Language? DetectedLanguage = null, 
    string? DetectedLanguageName = null);
