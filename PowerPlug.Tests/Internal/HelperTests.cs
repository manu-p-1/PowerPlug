using System.Text;
using PowerPlug.Internal;

namespace PowerPlug.Tests.Internal;

public class HashingTests
{
    [Theory]
    [InlineData("SHA256", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")]
    [InlineData("SHA1", "A9993E364706816ABA3E25717850C26C9CD0D89D")]
    [InlineData("MD5", "900150983CD24FB0D6963F7D28E17F72")]
    [InlineData("SHA384", "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7")]
    [InlineData("SHA512", "DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F")]
    public void ComputeHash_KnownVectors(string algorithm, string expected)
    {
        var actual = Hashing.ComputeHash(Encoding.ASCII.GetBytes("abc"), algorithm);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputeHash_IsCaseInsensitiveOnAlgorithmName()
    {
        var bytes = Encoding.ASCII.GetBytes("abc");
        Assert.Equal(Hashing.ComputeHash(bytes, "sha256"), Hashing.ComputeHash(bytes, "SHA256"));
    }

    [Fact]
    public void ComputeHash_UnknownAlgorithmThrows()
    {
        Assert.Throws<ArgumentException>(() => Hashing.ComputeHash([], "CRC32"));
    }

    [Fact]
    public void ComputeFileHash_MatchesByteHash()
    {
        using var temp = new Infrastructure.TempDirectory();
        var path = temp.WriteFile("f.txt", "abc");
        Assert.Equal(Hashing.ComputeHash(Encoding.ASCII.GetBytes("abc"), "SHA256"), Hashing.ComputeFileHash(path, "SHA256"));
    }

    [Theory]
    [InlineData("ab-cd", "abcd")]
    [InlineData("  AB CD  ", "ABCD")]
    [InlineData("ab:cd:ef", "abcdef")]
    [InlineData("plain", "plain")]
    public void NormalizeDigest_StripsSeparators(string input, string expected)
    {
        Assert.Equal(expected, Hashing.NormalizeDigest(input));
    }
}

public class Base64UrlTests
{
    [Fact]
    public void Encode_UsesUrlAlphabetWithoutPadding()
    {
        var encoded = Base64Url.Encode([0xfb, 0xff, 0xfe]);
        Assert.Equal("-__-", encoded);
        Assert.DoesNotContain('=', encoded);
    }

    [Theory]
    [InlineData("aGVsbG8=")]
    [InlineData("aGVsbG8")]
    [InlineData(" aGVsbG8 ")]
    public void DecodeFlexible_AcceptsStandardAndUnpadded(string input)
    {
        Assert.Equal("hello", Encoding.UTF8.GetString(Base64Url.DecodeFlexible(input)));
    }

    [Fact]
    public void DecodeFlexible_RoundTripsUrlSafe()
    {
        var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        Assert.Equal(bytes, Base64Url.DecodeFlexible(Base64Url.Encode(bytes)));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("!!!!")]
    public void DecodeFlexible_RejectsGarbage(string input)
    {
        Assert.Throws<FormatException>(() => Base64Url.DecodeFlexible(input));
    }
}

public class TextEncodingsTests
{
    [Theory]
    [InlineData("UTF8", "utf-8")]
    [InlineData("utf8", "utf-8")]
    [InlineData("ASCII", "us-ascii")]
    [InlineData("Unicode", "utf-16")]
    [InlineData("UTF32", "utf-32")]
    [InlineData("Latin1", "iso-8859-1")]
    public void Parse_ReturnsExpectedEncoding(string name, string webName)
    {
        Assert.Equal(webName, TextEncodings.Parse(name).WebName);
    }

    [Fact]
    public void Parse_Utf8HasNoPreamble()
    {
        Assert.Empty(TextEncodings.Parse("UTF8").GetPreamble());
    }
}

public class ByteSizeTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    [InlineData(-2048, "-2 KB")]
    [InlineData(long.MinValue, "-8 EB")]
    public void Format_UsesBinaryUnits(long bytes, string expected)
    {
        Assert.Equal(expected, ByteSize.Format(bytes));
    }
}

public class StatisticsTests
{
    [Fact]
    public void MedianOfSorted_OddCount()
    {
        Assert.Equal(3, Statistics.MedianOfSorted([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void MedianOfSorted_EvenCount()
    {
        Assert.Equal(2.5, Statistics.MedianOfSorted([1, 2, 3, 4]));
    }

    [Fact]
    public void MedianOfSorted_Empty()
    {
        Assert.Equal(0, Statistics.MedianOfSorted([]));
    }

    [Fact]
    public void StandardDeviation_PopulationFormula()
    {
        double[] values = [2, 4, 4, 4, 5, 5, 7, 9];
        Assert.Equal(2, Statistics.StandardDeviation(values, values.Average()), 10);
    }

    [Fact]
    public void Jitter_MeanAbsoluteConsecutiveDifference()
    {
        Assert.Equal(2, Statistics.Jitter([10, 12, 10, 12]));
        Assert.Equal(0, Statistics.Jitter([5]));
    }
}
