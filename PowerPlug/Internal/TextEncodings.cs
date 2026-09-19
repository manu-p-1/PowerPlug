using System.Text;

namespace PowerPlug.Internal;

/// <summary>
/// Maps the encoding names exposed on cmdlet parameters to <see cref="Encoding"/> instances.
/// </summary>
internal static class TextEncodings
{
    public const string Utf8 = "UTF8";
    public const string Ascii = "ASCII";
    public const string Unicode = "Unicode";
    public const string Utf32 = "UTF32";
    public const string Latin1 = "Latin1";

    /// <summary>
    /// Returns the encoding for a name from the ValidateSet. UTF8 is returned without a byte order mark.
    /// </summary>
    /// <exception cref="ArgumentException">The name is not one of the supported encodings.</exception>
    public static Encoding Parse(string name) => name.ToUpperInvariant() switch
    {
        "UTF8" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        "ASCII" => Encoding.ASCII,
        "UNICODE" => Encoding.Unicode,
        "UTF32" => Encoding.UTF32,
        "LATIN1" => Encoding.Latin1,
        _ => throw new ArgumentException($"Unsupported encoding: {name}", nameof(name)),
    };
}
