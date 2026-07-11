using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SoftScroll.Infrastructure;

/// <summary>
/// Single source of truth for the running app's version.
///
/// The version is read from assembly metadata at runtime, which is generated
/// from the &lt;Version&gt; property in SmoothScrollClone.csproj by the .NET SDK.
/// This means the displayed version is always in sync with the csproj — no
/// hand-editing of source files is required when bumping versions (whether by
/// a developer locally or by the release-please / build workflow in CI).
///
/// Source files that previously hard-coded the version string
/// (e.g. SettingsWindow.xaml.cs, installer/SoftScroll.iss) now derive it from
/// this helper or, for the installer, from the csproj via the build script.
/// </summary>
public static class AppVersion
{
    private static readonly Lazy<string> _informational = new(LoadInformationalVersion);
    private static readonly Lazy<string> _short = new(LoadShortVersion);

    /// <summary>
    /// Full informational version (e.g. "0.3.2" or "0.3.2+abc123" if a
    /// source-revision suffix was attached at build time).
    /// </summary>
    public static string Informational => _informational.Value;

    /// <summary>
    /// Short numeric version suitable for display (e.g. "0.3.2"). Strips any
    /// metadata suffix such as "+sha" or "-dirty" so the UI always shows a
    /// clean semver-like string.
    /// </summary>
    public static string Short => _short.Value;

    private static string LoadInformationalVersion()
    {
        try
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                var attr = entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (attr?.InformationalVersion is { Length: > 0 } v)
                    return v;
            }

            // Fallback: FileVersionInfo (works even for single-file published
            // apps where EntryAssembly may be null).
            var fvi = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
            if (!string.IsNullOrEmpty(fvi.ProductVersion))
                return fvi.ProductVersion;
            if (!string.IsNullOrEmpty(fvi.FileVersion))
                return fvi.FileVersion;
        }
        catch
        {
            // Intentionally swallowed — fall through to default below.
        }
        return "0.0.0";
    }

    private static string LoadShortVersion()
    {
        var raw = Informational;
        // Trim anything after '+' (semver build metadata) or '-' (pre-release).
        var plus = raw.IndexOf('+');
        if (plus >= 0) raw = raw[..plus];
        var dash = raw.IndexOf('-');
        if (dash >= 0) raw = raw[..dash];
        return raw;
    }
}