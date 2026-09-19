using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.Json;
using PowerPlug.Models;

namespace PowerPlug.Internal;

/// <summary>
/// Decodes JSON Web Tokens without verifying them. Useful for reading claims during development.
/// </summary>
internal static class JwtDecoder
{
    /// <summary>
    /// Decodes a compact JWS token (header.payload.signature).
    /// </summary>
    /// <exception cref="FormatException">The token is malformed.</exception>
    public static JwtToken Decode(string token, DateTimeOffset now)
    {
        var trimmed = token.Trim();
        if (trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["Bearer ".Length..].Trim();
        }

        var parts = trimmed.Split('.');
        if (parts.Length != 3)
        {
            throw new FormatException("A JWT must have three dot separated segments (header.payload.signature).");
        }

        OrderedDictionary header;
        OrderedDictionary payload;
        try
        {
            header = JsonToPowerShell.ParseObject(Encoding.UTF8.GetString(Base64Url.DecodeFlexible(parts[0])));
            payload = JsonToPowerShell.ParseObject(Encoding.UTF8.GetString(Base64Url.DecodeFlexible(parts[1])));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or DecoderFallbackException)
        {
            throw new FormatException("The JWT header or payload is not valid Base64Url encoded JSON.", ex);
        }

        var expires = GetTimeClaim(payload, "exp");

        return new JwtToken
        {
            Header = header,
            Payload = payload,
            Signature = parts[2],
            Algorithm = GetString(header, "alg"),
            Issuer = GetString(payload, "iss"),
            Subject = GetString(payload, "sub"),
            Audience = GetAudience(payload),
            IssuedAt = GetTimeClaim(payload, "iat"),
            NotBefore = GetTimeClaim(payload, "nbf"),
            ExpiresAt = expires,
            IsExpired = expires is not null && expires.Value <= now,
        };
    }

    private static string? GetString(OrderedDictionary dictionary, string key) =>
        dictionary.Contains(key) ? dictionary[key]?.ToString() : null;

    private static string? GetAudience(OrderedDictionary payload)
    {
        if (!payload.Contains("aud"))
        {
            return null;
        }

        return payload["aud"] switch
        {
            object?[] many => string.Join(", ", many.Select(a => a?.ToString())),
            var single => single?.ToString(),
        };
    }

    private static DateTimeOffset? GetTimeClaim(OrderedDictionary payload, string key)
    {
        if (!payload.Contains(key))
        {
            return null;
        }

        var seconds = payload[key] switch
        {
            long l => l,
            double d => (long)d,
            string s when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => (long?)null,
        };

        if (seconds is null)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
