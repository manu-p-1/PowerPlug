namespace PowerPlug.Internal;

/// <summary>
/// Base64Url (RFC 4648 section 5) helpers. Shared by the Base64 and JWT cmdlets.
/// </summary>
internal static class Base64Url
{
    /// <summary>
    /// Encodes bytes using the URL safe alphabet without padding.
    /// </summary>
    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// Decodes either standard Base64 or Base64Url, with or without padding.
    /// </summary>
    /// <exception cref="FormatException">The input is not valid Base64 in either alphabet.</exception>
    public static byte[] DecodeFlexible(string text)
    {
        var normalized = text.Trim().Replace('-', '+').Replace('_', '/');
        normalized = normalized.TrimEnd('=');

        var padding = normalized.Length % 4;
        if (padding == 1)
        {
            throw new FormatException("The input is not a valid Base64 string.");
        }

        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }
}
