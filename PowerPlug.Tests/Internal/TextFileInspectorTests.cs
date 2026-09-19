using System.Text;
using PowerPlug.Internal;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Internal;

public class TextFileInspectorTests
{
    private static byte[] Utf16(string text, bool bigEndian, bool bom)
    {
        var encoding = new UnicodeEncoding(bigEndian, bom);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(text)];
    }

    [Fact]
    public void Utf8WithBom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat("a\nb\n"u8.ToArray()).ToArray();
        var result = TextFileInspector.Inspect(bytes);
        Assert.Equal("UTF8-BOM", result.Encoding);
        Assert.True(result.HasBom);
        Assert.Equal(3, result.BomLength);
        Assert.Equal("LF", result.LineEnding);
        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void PlainAsciiIsReportedAsAscii()
    {
        var result = TextFileInspector.Inspect("hello\r\nworld\r\n"u8);
        Assert.Equal("ASCII", result.Encoding);
        Assert.False(result.HasBom);
        Assert.Equal("CRLF", result.LineEnding);
        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void Utf8WithoutBomIsRecognised()
    {
        var result = TextFileInspector.Inspect(Encoding.UTF8.GetBytes("caf\u00e9 \u2603\n"));
        Assert.Equal("UTF8", result.Encoding);
        Assert.False(result.IsBinary);
    }

    [Fact]
    public void InvalidUtf8HighBytesAreUnknown()
    {
        var result = TextFileInspector.Inspect(new byte[] { (byte)'a', 0xE9, (byte)'b', (byte)'\n' });
        Assert.Equal("Unknown", result.Encoding);
    }

    [Fact]
    public void TruncatedTrailingMultiByteSequenceIsTolerated()
    {
        var full = Encoding.UTF8.GetBytes("abc\u2603");
        var cut = full.AsSpan(0, full.Length - 1);
        Assert.Equal("UTF8", TextFileInspector.Inspect(cut, truncated: true).Encoding);
        Assert.Equal("Unknown", TextFileInspector.Inspect(cut, truncated: false).Encoding);
    }

    [Theory]
    [InlineData(false, "UTF16LE")]
    [InlineData(true, "UTF16BE")]
    public void Utf16WithBomCountsLineEndings(bool bigEndian, string expected)
    {
        var result = TextFileInspector.Inspect(Utf16("one\r\ntwo\nthree", bigEndian, bom: true));
        Assert.Equal(expected, result.Encoding);
        Assert.True(result.HasBom);
        Assert.Equal("Mixed", result.LineEnding);
        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void Utf32WithBom()
    {
        var encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes("x\ny\n")).ToArray();
        var result = TextFileInspector.Inspect(bytes);
        Assert.Equal("UTF32LE", result.Encoding);
        Assert.Equal("LF", result.LineEnding);
        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void NulByteMeansBinary()
    {
        var result = TextFileInspector.Inspect(new byte[] { 1, 2, 0, 3, (byte)'\n' });
        Assert.True(result.IsBinary);
        Assert.Equal("Binary", result.Encoding);
        Assert.Equal("None", result.LineEnding);
    }

    [Fact]
    public void EmptyFileIsAsciiWithNoLineEndings()
    {
        var result = TextFileInspector.Inspect([]);
        Assert.Equal("ASCII", result.Encoding);
        Assert.Equal("None", result.LineEnding);
        Assert.Equal(0, result.LineCount);
    }

    [Fact]
    public void Utf16WithoutBomIsDetectedFromZeroPattern()
    {
        var text = "The quick brown fox jumps over the lazy dog\r\n";
        Assert.Equal("UTF16LE", TextFileInspector.Inspect(Utf16(text, bigEndian: false, bom: false)).Encoding);
        Assert.Equal("UTF16BE", TextFileInspector.Inspect(Utf16(text, bigEndian: true, bom: false)).Encoding);
    }

    [Fact]
    public void TruncatedFlagIsCarriedThrough()
    {
        Assert.True(TextFileInspector.Inspect("abc"u8, truncated: true).Truncated);
        Assert.False(TextFileInspector.Inspect("abc"u8).Truncated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConvertLineEndings_Utf16KeepsBomAndWidth(bool bigEndian)
    {
        var encoding = new UnicodeEncoding(bigEndian, byteOrderMark: true);
        var input = Utf16("a\r\nb\rc\n", bigEndian, bom: true);
        var inspection = TextFileInspector.Inspect(input);

        var lf = TextFileInspector.ConvertLineEndings(input, inspection, "LF");
        Assert.Equal([.. encoding.GetPreamble(), .. encoding.GetBytes("a\nb\nc\n")], lf);

        var crlf = TextFileInspector.ConvertLineEndings(input, inspection, "CRLF");
        Assert.Equal([.. encoding.GetPreamble(), .. encoding.GetBytes("a\r\nb\r\nc\r\n")], crlf);
    }

    [Fact]
    public void LoneCarriageReturnsAreCounted()
    {
        var result = TextFileInspector.Inspect("a\rb\rc"u8);
        Assert.Equal("CR", result.LineEnding);
        Assert.Equal(2, result.LineCount);
    }

    [Fact]
    public void InspectFile_ReadsFromDisk()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("a.txt", "line1\r\nline2\r\n");
        var result = TextFileInspector.Inspect(path);
        Assert.Equal("CRLF", result.LineEnding);
        Assert.Equal("ASCII", result.Encoding);
    }

    [Theory]
    [InlineData("UTF8", 1, true)]
    [InlineData("UTF16LE", 2, true)]
    [InlineData("UTF16BE", 2, false)]
    [InlineData("UTF32LE", 4, true)]
    [InlineData("UTF32BE", 4, false)]
    public void Inspection_ReportsWidthAndEndianness(string name, int width, bool littleEndian)
    {
        var inspection = new TextFileInspector.Inspection(name, false, 0, "LF", false, 0, false);
        Assert.Equal(width, inspection.Width);
        Assert.Equal(littleEndian, inspection.LittleEndian);
    }
}
