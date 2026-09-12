using System.Runtime.InteropServices;

namespace PowerPlug.Internal;

/// <summary>
/// String comparer for file paths on the current platform. Windows and macOS are treated as case insensitive,
/// Linux as case sensitive. macOS volumes can be formatted either way, but insensitive is the default and the
/// safer assumption when deciding whether two names refer to the same item.
/// </summary>
internal static class PathComparer
{
    public static StringComparer Default { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}

/// <summary>
/// Compares file contents without hashing, stopping at the first block that differs.
/// </summary>
internal static class FileContent
{
    private const int BlockSize = 1 << 16;

    public static bool AreEqual(string left, string right)
    {
        using var a = new FileStream(left, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan);
        using var b = new FileStream(right, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan);

        if (a.Length != b.Length)
        {
            return false;
        }

        var bufferA = new byte[BlockSize];
        var bufferB = new byte[BlockSize];
        while (true)
        {
            var readA = a.ReadAtLeast(bufferA, BlockSize, throwOnEndOfStream: false);
            var readB = b.ReadAtLeast(bufferB, BlockSize, throwOnEndOfStream: false);
            if (readA != readB || !bufferA.AsSpan(0, readA).SequenceEqual(bufferB.AsSpan(0, readB)))
            {
                return false;
            }

            if (readA == 0)
            {
                return true;
            }
        }
    }
}
