using System.Collections.Specialized;
using System.Text.Json;

namespace PowerPlug.Internal;

/// <summary>
/// Converts System.Text.Json elements into the ordered hashtables and arrays PowerShell users expect.
/// </summary>
internal static class JsonToPowerShell
{
    /// <summary>
    /// Parses a JSON object document into an ordered dictionary.
    /// </summary>
    /// <exception cref="JsonException">The text is not valid JSON.</exception>
    /// <exception cref="InvalidOperationException">The root is not a JSON object.</exception>
    public static OrderedDictionary ParseObject(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Expected a JSON object.");
        }

        return ToDictionary(document.RootElement);
    }

    private static OrderedDictionary ToDictionary(JsonElement element)
    {
        var result = new OrderedDictionary(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ToValue(property.Value);
        }

        return result;
    }

    private static object? ToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ToDictionary(element),
        JsonValueKind.Array => element.EnumerateArray().Select(ToValue).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var l) => l,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };
}
