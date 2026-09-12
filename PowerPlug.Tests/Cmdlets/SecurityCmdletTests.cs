using System.Management.Automation;
using PowerPlug.Cmdlets.Security;
using PowerPlug.Internal;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class CompareHashCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _file;

    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    public CompareHashCmdletTests()
    {
        _file = _temp.WriteFile("abc.txt", "abc");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void MatchingSignatureIsCaseInsensitive()
    {
        var h = new CmdletHarness<CompareHashCmdlet>();
        h.Cmdlet.Path = _file;
        h.Cmdlet.Signature = AbcSha256.ToUpperInvariant();
        h.Run();

        var result = h.Only<HashComparisonResult>();
        Assert.True(result.Match);
        Assert.Equal("SHA256", result.Algorithm);
        Assert.Equal(_file, result.Path);
        Assert.Empty(h.Warnings);
    }

    [Fact]
    public void SignatureWithSeparatorsIsNormalised()
    {
        var h = new CmdletHarness<CompareHashCmdlet>();
        h.Cmdlet.Path = _file;
        h.Cmdlet.Signature = "BA78 16BF-8F01:CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";
        h.Run();
        Assert.True(h.Only<HashComparisonResult>().Match);
    }

    [Fact]
    public void MismatchWarnsAndReportsBothDigests()
    {
        var h = new CmdletHarness<CompareHashCmdlet>();
        h.Cmdlet.Path = _file;
        h.Cmdlet.Signature = "deadbeef";
        h.Run();

        var result = h.Only<HashComparisonResult>();
        Assert.False(result.Match);
        Assert.Equal("DEADBEEF", result.ExpectedHash);
        Assert.Equal(AbcSha256.ToUpperInvariant(), result.ComputedHash);
        Assert.Single(h.Warnings);
    }

    [Theory]
    [InlineData("MD5", "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData("sha1", "a9993e364706816aba3e25717850c26c9cd0d89d")]
    public void OtherAlgorithms(string algorithm, string signature)
    {
        var h = new CmdletHarness<CompareHashCmdlet>();
        h.Cmdlet.Algorithm = algorithm;
        h.Cmdlet.Path = _file;
        h.Cmdlet.Signature = signature;
        h.Run();
        var result = h.Only<HashComparisonResult>();
        Assert.True(result.Match);
        Assert.Equal(algorithm.ToUpperInvariant(), result.Algorithm);
    }

    [Fact]
    public void MissingFileWritesError()
    {
        var h = new CmdletHarness<CompareHashCmdlet>();
        h.Cmdlet.Path = _temp.Combine("missing.bin");
        h.Cmdlet.Signature = "00";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.ObjectNotFound, Assert.Single(h.Errors).CategoryInfo.Category);
    }
}

public class GetStringHashCmdletTests
{
    [Fact]
    public void HashesUtf8ByDefault()
    {
        var h = new CmdletHarness<GetStringHashCmdlet>();
        h.Cmdlet.InputString = "abc";
        h.Run();
        var result = h.Only<StringHashResult>();
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", result.Hash);
        Assert.Equal("SHA256", result.Algorithm);
        Assert.Equal("UTF8", result.Encoding);
    }

    [Fact]
    public void EmptyStringHasWellKnownHash()
    {
        var h = new CmdletHarness<GetStringHashCmdlet>();
        h.Cmdlet.InputString = string.Empty;
        h.Run();
        Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", h.Only<StringHashResult>().Hash);
    }

    [Theory]
    [InlineData("Unicode", "abc")]
    [InlineData("UTF32", "abc")]
    [InlineData("Latin1", "caf\u00e9")]
    public void EncodingChangesTheBytesThatAreHashed(string encoding, string input)
    {
        var h = new CmdletHarness<GetStringHashCmdlet>();
        h.Cmdlet.InputString = input;
        h.Cmdlet.Encoding = encoding;
        h.Run();

        var result = h.Only<StringHashResult>();
        var expected = Hashing.ComputeHash(TextEncodings.Parse(encoding).GetBytes(input), "SHA256");
        Assert.Equal(expected, result.Hash);
        Assert.Equal(encoding, result.Encoding);
        Assert.Equal(input, result.Input);
    }

    [Theory]
    [InlineData("MD5", 32)]
    [InlineData("sha1", 40)]
    [InlineData("SHA384", 96)]
    [InlineData("SHA512", 128)]
    public void AlgorithmSelectsDigestLength(string algorithm, int hexLength)
    {
        var h = new CmdletHarness<GetStringHashCmdlet>();
        h.Cmdlet.InputString = "abc";
        h.Cmdlet.Algorithm = algorithm;
        h.Run();
        var result = h.Only<StringHashResult>();
        Assert.Equal(hexLength, result.Hash.Length);
        Assert.Equal(algorithm.ToUpperInvariant(), result.Algorithm);
    }
}

public class NewRandomStringCmdletTests
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    [Fact]
    public void DefaultsTo16CharsFromLettersDigitsAndSymbols()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Run();
        var text = h.Only<string>();
        Assert.Equal(16, text.Length);
        Assert.All(text, c => Assert.Contains(c, Letters + Digits + Symbols));
    }

    [Fact]
    public void LongStringsFromTheFullPoolEventuallyUseSymbols()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 2000;
        h.Run();
        Assert.Contains(h.Only<string>(), c => Symbols.Contains(c, StringComparison.Ordinal));
    }

    [Fact]
    public void CountProducesDistinctStrings()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 32;
        h.Cmdlet.Count = 20;
        h.Run();
        var all = h.OutputOf<string>();
        Assert.Equal(20, all.Count);
        Assert.Equal(20, all.Distinct().Count());
    }

    [Fact]
    public void AlphanumericOnlyHasNoSymbols()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 500;
        h.Cmdlet.AlphanumericOnly = true;
        h.Run();
        Assert.All(h.Only<string>(), c => Assert.True(char.IsAsciiLetterOrDigit(c)));
    }

    [Fact]
    public void ExcludeAmbiguousDropsLookAlikes()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 2000;
        h.Cmdlet.ExcludeAmbiguous = true;
        h.Run();
        Assert.DoesNotContain(h.Only<string>(), c => "0OolI1".Contains(c, StringComparison.Ordinal));
    }

    [Fact]
    public void CustomCharacterSetIsHonouredAndDeduplicated()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 3000;
        h.Cmdlet.CharacterSet = "aab";
        h.Run();
        var text = h.Only<string>();
        Assert.All(text, c => Assert.Contains(c, "ab"));
        // With "aab" collapsed to "ab" the two characters should be roughly even, not 2:1.
        var ratio = text.Count(c => c == 'a') / (double)text.Length;
        Assert.InRange(ratio, 0.4, 0.6);
    }

    [Fact]
    public void AlphanumericOnlyAndExcludeAmbiguousCombine()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.Length = 2000;
        h.Cmdlet.AlphanumericOnly = true;
        h.Cmdlet.ExcludeAmbiguous = true;
        h.Run();
        var text = h.Only<string>();
        Assert.All(text, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
        Assert.DoesNotContain(text, c => "0OolI1".Contains(c, StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyPoolAfterFilteringWritesError()
    {
        var h = new CmdletHarness<NewRandomStringCmdlet>();
        h.Cmdlet.CharacterSet = "0O";
        h.Cmdlet.ExcludeAmbiguous = true;
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidArgument, h.OnlyError("EmptyCharacterPool").CategoryInfo.Category);
    }

    [Fact]
    public void Generate_UsesEveryCharacterEventually()
    {
        var text = NewRandomStringCmdlet.Generate("abc", 3000);
        Assert.Contains('a', text);
        Assert.Contains('b', text);
        Assert.Contains('c', text);
    }
}

public class TestElevationCmdletTests
{
    [Fact]
    public void MatchesTheRuntimeAnswer()
    {
        var h = new CmdletHarness<TestElevationCmdlet>();
        h.Run();
        Assert.Equal(Environment.IsPrivilegedProcess, h.Only<bool>());
    }
}
