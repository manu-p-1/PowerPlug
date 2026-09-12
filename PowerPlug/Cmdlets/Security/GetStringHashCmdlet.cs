using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Security;

/// <summary>
/// <para type="synopsis">Computes the hash of a string.</para>
/// <para type="description">Hashes a string. Get-FileHash only accepts files, so this covers cache keys, configuration
/// blobs and API signatures. Supports SHA256 (default), SHA384, SHA512, SHA1 and MD5.</para>
/// <example>
/// <para>Hash a string</para>
/// <code>"hello world" | Get-StringHash</code>
/// </example>
/// <example>
/// <para>Lower case MD5 for a legacy system</para>
/// <code>(Get-StringHash "hello" -Algorithm MD5).Hash.ToLower()</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "StringHash")]
[Alias("strhash")]
[OutputType(typeof(StringHashResult))]
public sealed class GetStringHashCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The text to hash.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [AllowEmptyString]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The hash algorithm. Defaults to SHA256.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateSet(Hashing.Sha256, Hashing.Sha384, Hashing.Sha512, Hashing.Sha1, Hashing.Md5, IgnoreCase = true)]
    public string Algorithm { get; set; } = Hashing.Sha256;

    /// <summary>
    /// <para type="description">Text encoding used to turn the string into bytes. Defaults to UTF8.</para>
    /// </summary>
    [Parameter]
    [ValidateSet(TextEncodings.Utf8, TextEncodings.Ascii, TextEncodings.Unicode, TextEncodings.Utf32, TextEncodings.Latin1)]
    public string Encoding { get; set; } = TextEncodings.Utf8;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var bytes = TextEncodings.Parse(Encoding).GetBytes(InputString);
        WriteObject(new StringHashResult
        {
            Algorithm = Algorithm.ToUpperInvariant(),
            Input = InputString,
            Encoding = Encoding,
            Hash = Hashing.ComputeHash(bytes, Algorithm),
        });
    }
}
