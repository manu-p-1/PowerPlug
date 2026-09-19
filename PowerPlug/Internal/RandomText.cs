using System.Security.Cryptography;
using System.Text;

namespace PowerPlug.Internal;

/// <summary>
/// Cryptographically secure character pool building and sampling, shared by New-RandomString and New-SecureKey.
/// Every character comes from <see cref="RandomNumberGenerator"/>, which draws from the OS CSPRNG and rejects
/// biased samples internally, so none of the methods here introduce modulo bias. Positions of "required"
/// characters are randomized with a full Fisher-Yates pass rather than left at fixed offsets, so a policy such
/// as "at least one symbol" does not leak the symbol's position or shrink the effective search space.
/// </summary>
internal static class RandomText
{
    public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Letters = Lowercase + Uppercase;
    public const string Digits = "0123456789";
    public const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";
    public const string Ambiguous = "0OolI1";

    /// <summary>
    /// Builds a de-duplicated character pool from the built-in sets or a custom set, honoring the exclusion flags.
    /// </summary>
    public static string BuildPool(string? customSet, bool alphanumericOnly, bool excludeAmbiguous)
    {
        var pool = customSet ?? (alphanumericOnly ? Letters + Digits : Letters + Digits + Symbols);
        if (excludeAmbiguous)
        {
            pool = new string([.. pool.Where(c => !Ambiguous.Contains(c, StringComparison.Ordinal))]);
        }

        return new string(pool.Distinct().ToArray());
    }

    /// <summary>
    /// Builds a string of the given length by sampling <paramref name="pool"/> uniformly at random.
    /// </summary>
    public static string GenerateUniform(string pool, int length)
    {
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a character array of the given length, sampled from <paramref name="pool"/>, that is guaranteed to
    /// contain at least the requested number of uppercase, lowercase, digit and symbol characters. The required
    /// characters are drawn from the intersection of the pool and the relevant class, the remaining positions are
    /// filled uniformly from the whole pool, and the entire buffer is then shuffled with a CSPRNG-driven
    /// Fisher-Yates pass. Callers own the returned array and should clear it once the secret is consumed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The requested minimums exceed <paramref name="length"/>, or the pool has no characters of a class that
    /// requires at least one.
    /// </exception>
    public static char[] GenerateWithRequirements(string pool, int length, int minUppercase, int minLowercase, int minDigits, int minSymbols)
    {
        var required = minUppercase + minLowercase + minDigits + minSymbols;
        if (required > length)
        {
            throw new ArgumentException(
                $"The requested minimum character counts ({required}) exceed the requested length ({length}).", nameof(length));
        }

        var buffer = new char[length];
        var index = 0;
        index = FillRequired(buffer, index, minUppercase, Intersect(pool, Uppercase), "uppercase");
        index = FillRequired(buffer, index, minLowercase, Intersect(pool, Lowercase), "lowercase");
        index = FillRequired(buffer, index, minDigits, Intersect(pool, Digits), "digit");
        index = FillRequired(buffer, index, minSymbols, Intersect(pool, Symbols), "symbol");

        for (; index < length; index++)
        {
            buffer[index] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }

        ShuffleInPlace(buffer);
        return buffer;
    }

    /// <summary>
    /// Shuffles a buffer in place using a cryptographically secure Durstenfeld (Fisher-Yates) shuffle. Every one
    /// of the length! permutations is equally likely because each swap index is drawn uniformly, without bias,
    /// from the CSPRNG.
    /// </summary>
    public static void ShuffleInPlace(char[] buffer)
    {
        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }

    private static int FillRequired(char[] buffer, int index, int count, string classPool, string className)
    {
        if (count == 0)
        {
            return index;
        }

        if (classPool.Length == 0)
        {
            throw new ArgumentException(
                $"The character pool does not contain any {className} characters to satisfy the minimum requirement.");
        }

        for (var i = 0; i < count; i++)
        {
            buffer[index++] = classPool[RandomNumberGenerator.GetInt32(classPool.Length)];
        }

        return index;
    }

    // classChars is one of the short constants above, so this never touches secret data.
    private static string Intersect(string pool, string classChars) =>
        new([.. classChars.Where(c => pool.Contains(c, StringComparison.Ordinal))]);
}
