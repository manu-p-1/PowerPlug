using System.Security.Cryptography;

namespace PowerPlug.Internal;

/// <summary>
/// Hash algorithm names accepted by PowerPlug cmdlets and helpers to compute them.
/// </summary>
internal static class Hashing
{
    public const string Sha256 = "SHA256";
    public const string Sha384 = "SHA384";
    public const string Sha512 = "SHA512";
    public const string Sha1 = "SHA1";
    public const string Md5 = "MD5";

    /// <summary>
    /// Hashes a stream and returns the upper case hex digest.
    /// </summary>
    public static string ComputeHash(Stream stream, string algorithm)
    {
        using var hasher = Create(algorithm);
        return Convert.ToHexString(hasher.ComputeHash(stream));
    }

    /// <summary>
    /// Hashes a byte array and returns the upper case hex digest.
    /// </summary>
    public static string ComputeHash(byte[] data, string algorithm)
    {
        using var hasher = Create(algorithm);
        return Convert.ToHexString(hasher.ComputeHash(data));
    }

    /// <summary>
    /// Hashes a file and returns the upper case hex digest.
    /// </summary>
    public static string ComputeFileHash(string path, string algorithm)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, FileOptions.SequentialScan);
        return ComputeHash(stream, algorithm);
    }

    /// <summary>
    /// Strips whitespace and dashes so digests copied from vendor pages compare cleanly.
    /// </summary>
    public static string NormalizeDigest(string digest) =>
        digest.Replace("-", string.Empty, StringComparison.Ordinal)
              .Replace(" ", string.Empty, StringComparison.Ordinal)
              .Replace(":", string.Empty, StringComparison.Ordinal)
              .Trim();

#pragma warning disable CA5350, CA5351 // SHA1 and MD5 are offered for verifying legacy vendor checksums only.
    private static HashAlgorithm Create(string algorithm) => algorithm.ToUpperInvariant() switch
    {
        Sha256 => SHA256.Create(),
        Sha384 => SHA384.Create(),
        Sha512 => SHA512.Create(),
        Sha1 => SHA1.Create(),
        Md5 => MD5.Create(),
        _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}", nameof(algorithm)),
    };
#pragma warning restore CA5350, CA5351
}
