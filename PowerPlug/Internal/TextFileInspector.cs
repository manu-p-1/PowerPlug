using System.Text;

namespace PowerPlug.Internal;

/// <summary>
/// Works out how a text file is encoded and which line endings it uses, and rewrites line endings without
/// decoding the text. CR and LF have the same byte values in ASCII, UTF-8 and every single byte code page,
/// and a fixed width in UTF-16 and UTF-32, so the bytes between them are never touched.
/// </summary>
internal static class TextFileInspector
{
    /// <summary>
    /// How many bytes are sampled to decide whether a file is text.
    /// </summary>
    public const int SampleSize = 64 * 1024;

    /// <summary>
    /// What the inspection found. <see cref="Truncated"/> is true when the file was longer than the sample, in
    /// which case <see cref="LineEnding"/> and <see cref="LineCount"/> describe the sample only.
    /// </summary>
    public sealed record Inspection(string Encoding, bool HasBom, int BomLength, string LineEnding, bool IsBinary, int LineCount, bool Truncated)
    {
        /// <summary>Bytes per code unit: 1, 2 or 4.</summary>
        public int Width => Encoding switch { "UTF16LE" or "UTF16BE" => 2, "UTF32LE" or "UTF32BE" => 4, _ => 1 };

        /// <summary>True when the low byte of each code unit comes first.</summary>
        public bool LittleEndian => Encoding is not ("UTF16BE" or "UTF32BE");
    }

    /// <summary>
    /// Inspects a file on disk.
    /// </summary>
    public static Inspection Inspect(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        var buffer = new byte[SampleSize];
        var read = stream.Read(buffer, 0, buffer.Length);
        return Inspect(buffer.AsSpan(0, read), stream.Length > read);
    }

    /// <summary>
    /// Inspects the leading bytes of a file. <paramref name="truncated"/> says whether more bytes follow.
    /// </summary>
    public static Inspection Inspect(ReadOnlySpan<byte> bytes, bool truncated = false)
    {
        var (encoding, bomLength) = DetectBom(bytes);
        var body = bytes[bomLength..];

        encoding ??= DetectWithoutBom(body, truncated);
        if (encoding == "Binary")
        {
            return new Inspection("Binary", false, 0, "None", true, 0, truncated);
        }

        var probe = new Inspection(encoding, bomLength > 0, bomLength, "None", false, 0, truncated);
        var (lineEnding, lines) = CountLineEndings(body, probe.Width, probe.LittleEndian);
        return probe with { LineEnding = lineEnding, LineCount = lines };
    }

    /// <summary>
    /// Rewrites every line ending in <paramref name="content"/> to <paramref name="to"/> ("LF" or "CRLF") using the
    /// code unit width from <paramref name="inspection"/>. The BOM, if any, is preserved. Returns the same array
    /// instance when nothing changed.
    /// </summary>
    public static byte[] ConvertLineEndings(byte[] content, Inspection inspection, string to)
    {
        var width = inspection.Width;
        var littleEndian = inspection.LittleEndian;
        var crlf = string.Equals(to, "CRLF", StringComparison.OrdinalIgnoreCase);
        var output = new MemoryStream(content.Length + (crlf ? content.Length / 16 : 0));
        var changed = false;

        output.Write(content, 0, inspection.BomLength);

        var body = content.AsSpan(inspection.BomLength);
        Span<byte> unit = stackalloc byte[width];
        for (var i = 0; i + width <= body.Length; i += width)
        {
            var isCr = IsChar(body, i, width, littleEndian, (byte)'\r');
            var isLf = !isCr && IsChar(body, i, width, littleEndian, (byte)'\n');

            if (!isCr && !isLf)
            {
                output.Write(body.Slice(i, width));
                continue;
            }

            var consumed = width;
            if (isCr && i + 2 * width <= body.Length && IsChar(body, i + width, width, littleEndian, (byte)'\n'))
            {
                consumed = 2 * width;
            }

            var wasCrlf = consumed == 2 * width;
            if (wasCrlf != crlf || (isCr && !wasCrlf))
            {
                changed = true;
            }

            if (crlf)
            {
                Encode(unit, width, littleEndian, (byte)'\r');
                output.Write(unit);
            }

            Encode(unit, width, littleEndian, (byte)'\n');
            output.Write(unit);
            i += consumed - width;
        }

        // Trailing bytes that do not fill a code unit are copied through unchanged.
        var tail = body.Length % width;
        if (tail > 0)
        {
            output.Write(body[^tail..]);
        }

        return changed ? output.ToArray() : content;
    }

    private static (string? Encoding, int BomLength) DetectBom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
        {
            return ("UTF32LE", 4);
        }

        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            return ("UTF32BE", 4);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return ("UTF8-BOM", 3);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return ("UTF16LE", 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return ("UTF16BE", 2);
        }

        return (null, 0);
    }

    private static string DetectWithoutBom(ReadOnlySpan<byte> body, bool truncated)
    {
        if (body.Length == 0)
        {
            return "ASCII";
        }

        // UTF-16 without a BOM (common from older Windows tools) shows up as a zero in every other byte.
        var pairs = Math.Min(body.Length, 4096) / 2;
        if (pairs >= 8)
        {
            var oddZeros = 0;
            var evenZeros = 0;
            for (var i = 0; i < pairs * 2; i += 2)
            {
                if (body[i] == 0)
                {
                    evenZeros++;
                }

                if (body[i + 1] == 0)
                {
                    oddZeros++;
                }
            }

            if (oddZeros > pairs * 0.7 && evenZeros < pairs * 0.1)
            {
                return "UTF16LE";
            }

            if (evenZeros > pairs * 0.7 && oddZeros < pairs * 0.1)
            {
                return "UTF16BE";
            }
        }

        // A NUL byte in the sample is the same rule git and grep use.
        if (body.IndexOf((byte)0) >= 0)
        {
            return "Binary";
        }

        if (body.IndexOfAnyExceptInRange((byte)0, (byte)127) < 0)
        {
            return "ASCII";
        }

        return IsValidUtf8(body, truncated) ? "UTF8" : "Unknown";
    }

    private static bool IsValidUtf8(ReadOnlySpan<byte> bytes, bool truncated)
    {
        // A sample may end mid character; drop up to three trailing bytes when more data follows.
        var end = bytes.Length;
        if (truncated)
        {
            var trim = 0;
            while (trim < 3 && end - trim > 0 && (bytes[end - trim - 1] & 0xC0) == 0x80)
            {
                trim++;
            }

            end -= trim + (end - trim > 0 && (bytes[end - trim - 1] & 0xC0) == 0xC0 ? 1 : 0);
        }

        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetCharCount(bytes[..Math.Max(0, end)]);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static (string LineEnding, int Lines) CountLineEndings(ReadOnlySpan<byte> bytes, int width, bool littleEndian)
    {
        var lf = 0;
        var crlf = 0;
        var cr = 0;

        for (var i = 0; i + width <= bytes.Length; i += width)
        {
            if (!IsChar(bytes, i, width, littleEndian, (byte)'\r'))
            {
                if (IsChar(bytes, i, width, littleEndian, (byte)'\n'))
                {
                    lf++;
                }

                continue;
            }

            if (i + 2 * width <= bytes.Length && IsChar(bytes, i + width, width, littleEndian, (byte)'\n'))
            {
                crlf++;
                i += width;
            }
            else
            {
                cr++;
            }
        }

        var kinds = (lf > 0 ? 1 : 0) + (crlf > 0 ? 1 : 0) + (cr > 0 ? 1 : 0);
        var style = kinds switch
        {
            0 => "None",
            1 when lf > 0 => "LF",
            1 when crlf > 0 => "CRLF",
            1 => "CR",
            _ => "Mixed",
        };

        return (style, lf + crlf + cr);
    }

    private static bool IsChar(ReadOnlySpan<byte> bytes, int index, int width, bool littleEndian, byte value)
    {
        var offset = littleEndian ? 0 : width - 1;
        if (bytes[index + offset] != value)
        {
            return false;
        }

        for (var k = 0; k < width; k++)
        {
            if (k != offset && bytes[index + k] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static void Encode(Span<byte> unit, int width, bool littleEndian, byte value)
    {
        unit.Clear();
        unit[littleEndian ? 0 : width - 1] = value;
    }
}
