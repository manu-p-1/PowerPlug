using System.Reflection;

namespace PowerPlug.Internal;

/// <summary>
/// Facts about the running PowerPlug assembly.
/// </summary>
internal static class ModuleInfo
{
    /// <summary>
    /// The module version for display, without the source control suffix the SDK appends.
    /// </summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var assembly = typeof(ModuleInfo).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}
