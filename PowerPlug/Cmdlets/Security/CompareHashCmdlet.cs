using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Security;

/// <summary>
/// <para type="synopsis">Compares a file's hash with a known signature.</para>
/// <para type="description">Hashes a file and compares the digest with the signature published alongside a download.
/// Dashes, colons and whitespace in the signature are ignored so values can be pasted straight from vendor pages.
/// Supports SHA256 (default), SHA384, SHA512, SHA1 and MD5. The weaker algorithms are there for checking older
/// checksums, not for security decisions.</para>
/// <example>
/// <para>Verify an installer</para>
/// <code>Compare-Hash -Path .\installer.exe -Signature 1f20cd15...927bc</code>
/// </example>
/// <example>
/// <para>Verify with MD5 and fail the script on mismatch</para>
/// <code>if (-not (Compare-Hash MD5 .\archive.zip $expected).Match) { throw "Checksum mismatch" }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Compare, "Hash")]
[Alias("csh")]
[OutputType(typeof(HashComparisonResult))]
public sealed class CompareHashCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The hash algorithm. Defaults to SHA256.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [Alias("HashType", "Hash")]
    [ValidateSet(Hashing.Sha256, Hashing.Sha384, Hashing.Sha512, Hashing.Sha1, Hashing.Md5, IgnoreCase = true)]
    public string Algorithm { get; set; } = Hashing.Sha256;

    /// <summary>
    /// <para type="description">The file to hash.</para>
    /// </summary>
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FilePath", "FullName")]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The expected digest.</para>
    /// </summary>
    [Parameter(Position = 2, Mandatory = true)]
    [Alias("KnownHash", "Expected")]
    [ValidateNotNullOrEmpty]
    public string Signature { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var resolved = ResolvePath(Path);
        if (!File.Exists(resolved))
        {
            WriteFileError(new FileNotFoundException($"File not found: {resolved}"), resolved);
            return;
        }

        string computed;
        try
        {
            computed = Hashing.ComputeFileHash(resolved, Algorithm);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, resolved);
            return;
        }

        var expected = Hashing.NormalizeDigest(Signature).ToUpperInvariant();
        var match = string.Equals(computed, expected, StringComparison.Ordinal);

        if (!match)
        {
            WriteWarning($"Hash mismatch for {resolved}");
        }

        WriteObject(new HashComparisonResult
        {
            Algorithm = Algorithm.ToUpperInvariant(),
            Path = resolved,
            ComputedHash = computed,
            ExpectedHash = expected,
            Match = match,
        });
    }
}
