using System.Text;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Parses .env files the way the popular dotenv libraries do: KEY=VALUE lines, optional "export " prefix,
/// comments, single and double quoted values, escape sequences in double quotes and multi line quoted values.
/// </summary>
internal static class DotEnvParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly Regex KeyPattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant, RegexTimeout);
    // A backslash before the dollar sign means "literal"; Unescape turns it into a plain $ afterwards.
    private static readonly Regex ExpandPattern = new(@"(?<!\\)\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}|(?<!\\)\$(?<bare>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>
    /// Parses .env content into ordered key/value pairs. Later keys override earlier ones.
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <param name="expand">Expand ${VAR} and $VAR references using earlier keys, then the environment.</param>
    /// <exception cref="FormatException">A line is not a comment, blank, or KEY=VALUE.</exception>
    public static List<KeyValuePair<string, string>> Parse(string content, bool expand = false)
    {
        var result = new List<KeyValuePair<string, string>>();
        var lookup = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var equals = line.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0)
            {
                throw new FormatException($"Line {i + 1} is not in KEY=VALUE form: {lines[i]}");
            }

            var key = line[..equals].Trim();
            if (!KeyPattern.IsMatch(key))
            {
                throw new FormatException($"Line {i + 1} has an invalid key '{key}'.");
            }

            var rawValue = line[(equals + 1)..].TrimStart();
            var (value, quoted) = ReadValue(rawValue, lines, ref i);

            // Expand before unescaping so "\$HOME" stays literal. Single quotes never expand.
            if (expand && quoted != '\'')
            {
                value = Expand(value, lookup);
            }

            if (quoted == '"')
            {
                value = Unescape(value);
            }

            lookup[key] = value;
            result.RemoveAll(kv => kv.Key == key);
            result.Add(new KeyValuePair<string, string>(key, value));
        }

        return result;
    }

    // Returns the raw value (escapes still in place) and the quote character used, or '\0' when unquoted.
    // Advances the line index when a quoted value spans several lines.
    private static (string Value, char Quote) ReadValue(string raw, string[] lines, ref int index)
    {
        if (raw.Length == 0)
        {
            return (string.Empty, '\0');
        }

        var quote = raw[0];
        if (quote is not ('"' or '\''))
        {
            var hash = raw.IndexOf(" #", StringComparison.Ordinal);
            return ((hash >= 0 ? raw[..hash] : raw).Trim(), '\0');
        }

        var builder = new StringBuilder();
        var text = raw[1..];
        while (true)
        {
            var end = FindClosingQuote(text, quote);
            if (end >= 0)
            {
                builder.Append(text, 0, end);
                break;
            }

            builder.Append(text).Append('\n');
            index++;
            if (index >= lines.Length)
            {
                throw new FormatException("A quoted value is missing its closing quote.");
            }

            text = lines[index];
        }

        var value = builder.ToString();
        return (value, quote);
    }

    private static int FindClosingQuote(string text, char quote)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && quote == '"')
            {
                i++;
                continue;
            }

            if (text[i] == quote)
            {
                return i;
            }
        }

        return -1;
    }

    private static string Unescape(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }

            i++;
            builder.Append(value[i] switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '"' => '"',
                '\\' => '\\',
                '$' => '$',
                var other => other,
            });
        }

        return builder.ToString();
    }

    private static string Expand(string value, Dictionary<string, string> lookup) =>
        ExpandPattern.Replace(value, match =>
        {
            var name = match.Groups["name"].Success ? match.Groups["name"].Value : match.Groups["bare"].Value;
            return lookup.TryGetValue(name, out var local)
                ? local
                : Environment.GetEnvironmentVariable(name) ?? string.Empty;
        });
}
