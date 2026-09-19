using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Reads and writes the alias lines that the Byname cmdlets persist to a PowerShell profile.
/// Each Byname is stored as a plain New-Alias or Set-Alias line. If the alias points at a function
/// defined in the session, the function definition is stored right above it so it exists at startup.
/// </summary>
internal sealed class ProfileAliasWriter
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

    // Appended to function definitions PowerPlug writes so removal never touches hand written functions.
    internal const string FunctionMarker = "# PowerPlug Byname";

    /// <summary>
    /// The profile file this writer operates on.
    /// </summary>
    public string ProfilePath { get; }

    public ProfileAliasWriter(string profilePath)
    {
        if (string.IsNullOrWhiteSpace(profilePath))
        {
            throw new ArgumentException("Profile path is required.", nameof(profilePath));
        }

        ProfilePath = profilePath;
    }

    /// <summary>
    /// True when the profile file exists on disk.
    /// </summary>
    public bool Exists => File.Exists(ProfilePath);

    /// <summary>
    /// Creates the profile file and its folder when missing.
    /// </summary>
    public void EnsureExists()
    {
        if (Exists)
        {
            return;
        }

        var directory = Path.GetDirectoryName(ProfilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ProfilePath, string.Empty);
    }

    /// <summary>
    /// Appends a Byname entry. The function body is written first when provided and not already present.
    /// </summary>
    /// <param name="aliasCommand">The New-Alias or Set-Alias line produced by <see cref="FormatAliasCommand"/>.</param>
    /// <param name="functionName">The function the alias points at, if the value resolved to a function.</param>
    /// <param name="functionBody">The function body to persist alongside the alias.</param>
    public void Append(string aliasCommand, string? functionName = null, string? functionBody = null)
    {
        EnsureExists();

        var existing = File.ReadAllText(ProfilePath);
        var builder = new StringBuilder(existing.TrimEnd());
        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        if (!string.IsNullOrEmpty(functionName) && functionBody is not null && !ContainsFunction(existing, functionName))
        {
            builder.Append("function ").Append(functionName).Append(" { ").Append(functionBody.Trim()).Append(" } ").Append(FunctionMarker).AppendLine();
        }

        builder.Append(aliasCommand).AppendLine();
        File.WriteAllText(ProfilePath, builder.ToString());
    }

    /// <summary>
    /// Removes every alias line for the given name. Function definitions that PowerPlug wrote are removed too,
    /// but only when no other alias line in the profile still points at them. Hand written functions are left alone.
    /// </summary>
    /// <returns>The number of alias lines removed.</returns>
    public int Remove(string aliasName)
    {
        if (!Exists)
        {
            return 0;
        }

        var text = File.ReadAllText(ProfilePath);
        var aliasRegex = AliasLineRegex(aliasName);

        int removed;
        try
        {
            var matches = aliasRegex.Matches(text);
            removed = matches.Count;
            if (removed == 0)
            {
                return 0;
            }

            var candidates = matches.Select(ExtractValue).Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.Ordinal).ToList();
            text = aliasRegex.Replace(text, string.Empty);

            foreach (var functionName in candidates)
            {
                if (!AnyAliasReferences(text, functionName))
                {
                    text = MarkedFunctionRegex(functionName).Replace(text, string.Empty);
                }
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new InvalidOperationException("The profile could not be scanned in time. It may contain unusually complex content.", ex);
        }

        text = text.TrimEnd();
        File.WriteAllText(ProfilePath, text.Length == 0 ? string.Empty : text + Environment.NewLine);
        return removed;
    }

    /// <summary>
    /// True when the profile defines a function with this name.
    /// </summary>
    public bool ContainsFunction(string functionName) =>
        Exists && ContainsFunction(File.ReadAllText(ProfilePath), functionName);

    /// <summary>
    /// Builds the alias command line that gets persisted. Values with whitespace are single quoted so the
    /// line stays valid PowerShell.
    /// </summary>
    public static string FormatAliasCommand(
        string verb,
        string name,
        string value,
        ScopedItemOptions option,
        string scope,
        bool force,
        string? description)
    {
        var builder = new StringBuilder()
            .Append(verb).Append("-Alias")
            .Append(" -Name ").Append(Quote(name))
            .Append(" -Value ").Append(Quote(value))
            .Append(" -Option ").Append(Quote(option.ToString()))
            .Append(" -Scope ").Append(scope);

        if (force)
        {
            builder.Append(" -Force");
        }

        if (!string.IsNullOrEmpty(description))
        {
            builder.Append(" -Description ").Append(Quote(description, alwaysQuote: true));
        }

        return builder.ToString();
    }

    private static string Quote(string text, bool alwaysQuote = false)
    {
        var needsQuotes = alwaysQuote || text.Length == 0 || text.Any(c => char.IsWhiteSpace(c) || c is '\'' or '"' or '`' or '$' or ';' or '#');
        return needsQuotes ? "'" + text.Replace("'", "''", StringComparison.Ordinal) + "'" : text;
    }

    private static bool ContainsFunction(string profileText, string functionName)
    {
        try
        {
            return FunctionRegex(functionName, requireMarker: false).IsMatch(profileText);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool AnyAliasReferences(string profileText, string value)
    {
        var escaped = Regex.Escape(value);
        var pattern = $@"^[ \t]*(?:New|Set)-Alias\s+-Name\s+\S+\s+-Value\s+(?:'{escaped}'|""{escaped}""|{escaped})(?:\s|$)";
        return new Regex(pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant, RegexTimeout).IsMatch(profileText);
    }

    private static string ExtractValue(Match match)
    {
        for (var i = 1; i <= 3; i++)
        {
            if (match.Groups[i].Success)
            {
                var raw = match.Groups[i].Value;
                return i == 1 ? raw.Replace("''", "'", StringComparison.Ordinal) : raw;
            }
        }

        return string.Empty;
    }

    // Matches a whole New-Alias/Set-Alias line for the alias name, capturing the value as
    // group 1 (single quoted), 2 (double quoted) or 3 (bare word).
    private static Regex AliasLineRegex(string aliasName)
    {
        var name = Regex.Escape(aliasName);
        var pattern =
            $@"^[ \t]*(?:New|Set)-Alias\s+-Name\s+(?:'{name}'|""{name}""|{name})\s+-Value\s+(?:'((?:[^']|'')*)'|""([^""]*)""|(\S+))[^\r\n]*(?:\r?\n|$)";
        return new Regex(pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant, RegexTimeout);
    }

    // Matches "function <name> { ... }" with balanced braces so nested blocks are removed in full. When the marker
    // is required only definitions PowerPlug wrote match.
    private static Regex FunctionRegex(string functionName, bool requireMarker)
    {
        var name = Regex.Escape(functionName);
        var trailer = requireMarker ? $@"[ \t]*{Regex.Escape(FunctionMarker)}[ \t]*" : @"[^\r\n]*";
        var pattern =
            $@"^[ \t]*function\s+{name}\s*\{{(?>[^{{}}]+|\{{(?<depth>)|\}}(?<-depth>))*(?(depth)(?!))\}}{trailer}(?:\r?\n|$)";
        return new Regex(pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant, RegexTimeout);
    }

    private static Regex MarkedFunctionRegex(string functionName) => FunctionRegex(functionName, requireMarker: true);
}
