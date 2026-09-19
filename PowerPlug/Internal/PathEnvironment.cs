using System.Runtime.InteropServices;

namespace PowerPlug.Internal;

/// <summary>
/// Split, join and compare PATH entries the way the current platform does.
/// </summary>
internal static class PathEnvironment
{
    /// <summary>
    /// Case insensitive on Windows and macOS, case sensitive on Linux.
    /// </summary>
    public static StringComparer Comparer { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Splits a PATH value into entries, dropping empty ones.
    /// </summary>
    public static List<string> Split(string? value) =>
        string.IsNullOrEmpty(value)
            ? []
            : value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>
    /// Joins entries back into a PATH value.
    /// </summary>
    public static string Join(IEnumerable<string> entries) => string.Join(Path.PathSeparator, entries);

    /// <summary>
    /// Normalises an entry for comparison: trims whitespace and trailing separators, and expands to a full path
    /// when the entry is rooted.
    /// </summary>
    public static string Normalize(string entry)
    {
        var trimmed = entry.Trim();
        if (trimmed.Length > 1)
        {
            trimmed = trimmed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        if (Path.IsPathRooted(trimmed))
        {
            try
            {
                trimmed = Path.GetFullPath(trimmed);
                if (trimmed.Length > 1)
                {
                    trimmed = trimmed.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
                // Leave odd entries as they are so they still show up (and can be removed).
            }
        }

        return trimmed;
    }

    /// <summary>
    /// True when two entries point at the same directory.
    /// </summary>
    public static bool Same(string a, string b) => Comparer.Equals(Normalize(a), Normalize(b));

    /// <summary>
    /// Maps the Target parameter value to the .NET enum.
    /// </summary>
    public static EnvironmentVariableTarget ParseTarget(string target) => target switch
    {
        "User" => EnvironmentVariableTarget.User,
        "Machine" => EnvironmentVariableTarget.Machine,
        _ => EnvironmentVariableTarget.Process,
    };

    /// <summary>
    /// User and Machine targets are stored in the registry and only exist on Windows.
    /// </summary>
    public static bool TargetSupported(EnvironmentVariableTarget target) =>
        target == EnvironmentVariableTarget.Process || RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
