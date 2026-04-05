using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace SoftScroll.Infrastructure;

public static class LocalizationManager
{
    public static readonly string[] SupportedLanguages = { "en", "vi", "zh" };
    public static readonly string[] LanguageDisplayNames = { "English", "Tiếng Việt", "中文" };

    private static readonly ResourceManager _rm = new("SoftScroll.Resources.Strings", typeof(LocalizationManager).Assembly);
    private static CultureInfo _culture = CultureInfo.InvariantCulture;

    public static string CurrentLanguage { get; private set; } = "en";

    public static void SetLanguage(string langCode)
    {
        langCode = langCode?.ToLowerInvariant() ?? "en";
        if (Array.IndexOf(SupportedLanguages, langCode) < 0)
            langCode = "en";

        CurrentLanguage = langCode;
        _culture = langCode == "en"
            ? CultureInfo.InvariantCulture
            : new CultureInfo(langCode);

        Thread.CurrentThread.CurrentUICulture = _culture;
    }

    public static string Get(string key)
    {
        return _rm.GetString(key, _culture) ?? key;
    }

    public static int GetLanguageIndex()
    {
        var idx = Array.IndexOf(SupportedLanguages, CurrentLanguage);
        return idx >= 0 ? idx : 0;
    }
}
