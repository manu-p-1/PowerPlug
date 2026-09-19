using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using PowerPlug.Cmdlets.Data;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class ConvertToBase64CmdletTests
{
    [Fact]
    public void EncodesStringWithUtf8ByDefault()
    {
        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = "Hello, World!";
        h.Run();
        Assert.Equal("SGVsbG8sIFdvcmxkIQ==", h.Only<string>());
    }

    [Fact]
    public void EncodesEmptyString()
    {
        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = string.Empty;
        h.Run();
        Assert.Equal(string.Empty, h.Only<string>());
    }

    [Fact]
    public void UrlSafeUsesAlternateAlphabet()
    {
        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = "\u00fb\u00ff\u00fe";
        h.Cmdlet.Encoding = "Latin1";
        h.Cmdlet.UrlSafe = true;
        h.Run();
        Assert.Equal("-__-", h.Only<string>());
    }

    [Fact]
    public void UnicodeEncodingChangesOutput()
    {
        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = "A";
        h.Cmdlet.Encoding = "Unicode";
        h.Run();
        Assert.Equal("QQA=", h.Only<string>());
    }

    [Fact]
    public void EncodesFileBytes()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteBytes("bin.dat", [1, 2, 3, 4]);

        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("File");
        h.Cmdlet.Path = path;
        h.Run();
        Assert.Equal("AQIDBA==", h.Only<string>());
    }

    [Fact]
    public void MissingFileWritesError()
    {
        var h = new CmdletHarness<ConvertToBase64Cmdlet>().WithParameterSet("File");
        h.Cmdlet.Path = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid());
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal("PathNotFound", Assert.Single(h.Errors).FullyQualifiedErrorId.Split(',')[0]);
    }
}

public class ConvertFromBase64CmdletTests
{
    [Theory]
    [InlineData("SGVsbG8sIFdvcmxkIQ==", "UTF8", "Hello, World!")]
    [InlineData("SGVsbG8sIFdvcmxkIQ", "UTF8", "Hello, World!")]
    [InlineData("-__-", "Latin1", "\u00fb\u00ff\u00fe")]
    [InlineData("QQBCAA==", "Unicode", "AB")]
    [InlineData("QUI=", "ASCII", "AB")]
    public void DecodesStandardAndUrlSafe(string input, string encoding, string expected)
    {
        var h = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = input;
        h.Cmdlet.Encoding = encoding;
        h.Run();
        Assert.Equal(expected, h.Only<string>());
    }

    [Fact]
    public void InvalidInputWritesErrorInsteadOfThrowing()
    {
        var h = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("String");
        h.Cmdlet.InputString = "!!!not base64";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidData, h.OnlyError("InvalidBase64").CategoryInfo.Category);
    }

    [Fact]
    public void WhatIfDoesNotWriteTheFile()
    {
        using var temp = new TempDirectory();
        var target = temp.Combine("out.bin");

        var h = new CmdletHarness<ConvertFromBase64Cmdlet>(shouldProcess: false).WithParameterSet("File");
        h.Cmdlet.InputString = "AQID";
        h.Cmdlet.OutputPath = target;
        h.Run();

        Assert.False(File.Exists(target));
        Assert.Equal([target], h.ShouldProcessTargets);
    }

    [Fact]
    public void WriteToMissingDirectoryWritesError()
    {
        using var temp = new TempDirectory();
        var h = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("File");
        h.Cmdlet.InputString = "AQID";
        h.Cmdlet.OutputPath = temp.Combine(Path.Combine("missing-dir", "out.bin"));
        h.Run();

        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.ObjectNotFound, h.OnlyError("PathNotFound").CategoryInfo.Category);
    }

    [Fact]
    public void WritesBytesToFileAndReturnsFileInfo()
    {
        using var temp = new TempDirectory();
        var target = temp.Combine("out.bin");

        var h = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("File");
        h.Cmdlet.InputString = "AQIDBA==";
        h.Cmdlet.OutputPath = target;
        h.Run();

        Assert.Equal(target, h.Only<FileInfo>().FullName);
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(target));
    }

    [Fact]
    public void RefusesToOverwriteWithoutForce()
    {
        using var temp = new TempDirectory();
        var target = temp.WriteFile("out.bin", "old");

        var h = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("File");
        h.Cmdlet.InputString = "AQID";
        h.Cmdlet.OutputPath = target;
        h.Run();

        Assert.Equal(ErrorCategory.ResourceExists, Assert.Single(h.Errors).CategoryInfo.Category);
        Assert.Equal("old", File.ReadAllText(target));

        var forced = new CmdletHarness<ConvertFromBase64Cmdlet>().WithParameterSet("File");
        forced.Cmdlet.InputString = "AQID";
        forced.Cmdlet.OutputPath = target;
        forced.Cmdlet.Force = true;
        forced.Run();
        Assert.Empty(forced.Errors);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(target));
    }
}

public class UrlEncodingCmdletTests
{
    [Fact]
    public void ToUrlEncoding_Rfc3986ByDefault()
    {
        var h = new CmdletHarness<ConvertToUrlEncodingCmdlet>();
        h.Cmdlet.InputString = "a b&c=d/é";
        h.Run();
        Assert.Equal("a%20b%26c%3Dd%2F%C3%A9", h.Only<string>());
    }

    [Fact]
    public void ToUrlEncoding_FormUsesPlus()
    {
        var h = new CmdletHarness<ConvertToUrlEncodingCmdlet>();
        h.Cmdlet.InputString = "a b";
        h.Cmdlet.Form = true;
        h.Run();
        Assert.Equal("a+b", h.Only<string>());
    }

    [Fact]
    public void FromUrlEncoding_LeavesPlusAloneUnlessForm()
    {
        var h = new CmdletHarness<ConvertFromUrlEncodingCmdlet>();
        h.Cmdlet.InputString = "a+b%20c";
        h.Run();
        Assert.Equal("a+b c", h.Only<string>());

        var form = new CmdletHarness<ConvertFromUrlEncodingCmdlet>();
        form.Cmdlet.InputString = "a+b%20c";
        form.Cmdlet.Form = true;
        form.Run();
        Assert.Equal("a b c", form.Only<string>());
    }

    [Fact]
    public void RoundTrip()
    {
        const string original = "name=Zürich & co/100%";
        var enc = new CmdletHarness<ConvertToUrlEncodingCmdlet>();
        enc.Cmdlet.InputString = original;
        enc.Run();

        var dec = new CmdletHarness<ConvertFromUrlEncodingCmdlet>();
        dec.Cmdlet.InputString = enc.Only<string>();
        dec.Run();
        Assert.Equal(original, dec.Only<string>());
    }
}

public class UnixTimeCmdletTests
{
    [Fact]
    public void FromUnixTime_SecondsToUtc()
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = 1700000000;
        h.Cmdlet.Utc = true;
        h.Run();
        Assert.Equal(new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc), h.Only<DateTime>());
    }

    [Fact]
    public void FromUnixTime_AutoDetectsMilliseconds()
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = 1700000000123;
        h.Cmdlet.Utc = true;
        h.Run();
        Assert.Equal(new DateTime(2023, 11, 14, 22, 13, 20, 123, DateTimeKind.Utc), h.Only<DateTime>());
    }

    [Fact]
    public void FromUnixTime_UnitOverridesDetection()
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = 1700000000;
        h.Cmdlet.Unit = "Milliseconds";
        h.Cmdlet.Utc = true;
        h.Run();
        Assert.Equal(1970, h.Only<DateTime>().Year);
    }

    [Fact]
    public void FromUnixTime_LocalByDefault()
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = 0;
        h.Run();
        Assert.Equal(DateTimeKind.Local, h.Only<DateTime>().Kind);
    }

    [Theory]
    [InlineData(long.MaxValue, "Seconds")]
    [InlineData(long.MinValue, "Auto")]
    [InlineData(long.MaxValue, "Milliseconds")]
    public void FromUnixTime_OutOfRangeWritesError(long timestamp, string unit)
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = timestamp;
        h.Cmdlet.Unit = unit;
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidArgument, h.OnlyError("TimestampOutOfRange").CategoryInfo.Category);
    }

    [Fact]
    public void FromUnixTime_NegativeTimestampsArePre1970()
    {
        var h = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        h.Cmdlet.Timestamp = -86400;
        h.Cmdlet.Utc = true;
        h.Run();
        Assert.Equal(new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Utc), h.Only<DateTime>());
    }

    [Fact]
    public void ToUnixTime_UnspecifiedKindIsTreatedAsLocal()
    {
        var local = new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Unspecified);
        var h = new CmdletHarness<ConvertToUnixTimeCmdlet>();
        h.Cmdlet.Date = local;
        h.Run();
        Assert.Equal(new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUnixTimeSeconds(), h.Only<long>());
    }

    [Fact]
    public void ToUnixTime_UtcDate()
    {
        var h = new CmdletHarness<ConvertToUnixTimeCmdlet>();
        h.Cmdlet.Date = new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc);
        h.Run();
        Assert.Equal(1700000000L, h.Only<long>());
    }

    [Fact]
    public void ToUnixTime_Milliseconds()
    {
        var h = new CmdletHarness<ConvertToUnixTimeCmdlet>();
        h.Cmdlet.Date = new DateTime(2023, 11, 14, 22, 13, 20, 123, DateTimeKind.Utc);
        h.Cmdlet.Milliseconds = true;
        h.Run();
        Assert.Equal(1700000000123L, h.Only<long>());
    }

    [Fact]
    public void ToUnixTime_DefaultsToNow()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var h = new CmdletHarness<ConvertToUnixTimeCmdlet>();
        h.Run();
        Assert.InRange(h.Only<long>(), before, before + 5);
    }

    [Fact]
    public void RoundTrip()
    {
        var to = new CmdletHarness<ConvertToUnixTimeCmdlet>();
        to.Cmdlet.Date = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        to.Run();

        var from = new CmdletHarness<ConvertFromUnixTimeCmdlet>();
        from.Cmdlet.Timestamp = to.Only<long>();
        from.Cmdlet.Utc = true;
        from.Run();
        Assert.Equal(to.Cmdlet.Date, from.Only<DateTime>());
    }
}

public class ConvertFromJwtCmdletTests
{
    [Fact]
    public void DecodesToken()
    {
        var h = new CmdletHarness<ConvertFromJwtCmdlet>();
        h.Cmdlet.Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        h.Run();
        var token = h.Only<JwtToken>();
        Assert.Equal("HS256", token.Algorithm);
        Assert.Equal("John Doe", token.Payload["name"]);
    }

    [Fact]
    public void MalformedTokenWritesError()
    {
        var h = new CmdletHarness<ConvertFromJwtCmdlet>();
        h.Cmdlet.Token = "nope";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidData, h.OnlyError("InvalidJwt").CategoryInfo.Category);
    }
}

public class ConvertColorCmdletTests
{
    [Theory]
    [InlineData("#FF8800")]
    [InlineData("rgb(255, 136, 0)")]
    [InlineData("hsl(32, 100%, 50%)")]
    public void ParsesTextNotations(string input)
    {
        var h = new CmdletHarness<ConvertColorCmdlet>().WithParameterSet("Text");
        h.Cmdlet.InputString = input;
        h.Run();
        Assert.Equal("#FF8800", h.Only<ColorInfo>().Hex);
    }

    [Fact]
    public void FromChannelsWithAlpha()
    {
        var h = new CmdletHarness<ConvertColorCmdlet>().WithParameterSet("Rgb");
        h.Cmdlet.R = 255;
        h.Cmdlet.G = 136;
        h.Cmdlet.B = 0;
        h.Cmdlet.A = 128;
        h.Run();
        var color = h.Only<ColorInfo>();
        Assert.Equal("#FF880080", color.Hex);
        Assert.Equal("rgba(255, 136, 0, 0.5)", color.Rgb);
    }

    [Fact]
    public void FromHsl()
    {
        var h = new CmdletHarness<ConvertColorCmdlet>().WithParameterSet("Hsl");
        h.Cmdlet.H = 32;
        h.Cmdlet.S = 100;
        h.Cmdlet.L = 50;
        h.Run();
        Assert.Equal("#FF8800", h.Only<ColorInfo>().Hex);
    }

    [Fact]
    public void InvalidTextWritesError()
    {
        var h = new CmdletHarness<ConvertColorCmdlet>().WithParameterSet("Text");
        h.Cmdlet.InputString = "not a colour";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidArgument, h.OnlyError("InvalidColor").CategoryInfo.Category);
    }
}

public class ConvertToHashTableCmdletTests
{
    private static PSObject Custom(params (string Name, object? Value)[] props)
    {
        var pso = new PSObject();
        foreach (var (name, value) in props)
        {
            pso.Properties.Add(new PSNoteProperty(name, value));
        }

        return pso;
    }

    [Fact]
    public void ConvertsNotePropertiesInOrder()
    {
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = Custom(("Zeta", 1), ("Alpha", "two"));
        h.Run();

        var ht = h.Only<OrderedDictionary>();
        Assert.Equal(["Zeta", "Alpha"], ht.Keys.Cast<string>().ToArray());
        Assert.Equal(1, ht["Zeta"]);
        Assert.Equal("two", ht["Alpha"]);
    }

    [Fact]
    public void WithoutRecurseNestedObjectsAreLeftAlone()
    {
        var inner = Custom(("X", 1));
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = Custom(("Inner", (object?)inner));
        h.Run();

        Assert.Same(inner, h.Only<OrderedDictionary>()["Inner"]);
    }

    [Fact]
    public void RecurseConvertsNestedObjectsAndArrays()
    {
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = Custom(
            ("Inner", Custom(("X", 1))),
            ("List", new object[] { Custom(("Y", 2)), "str", 3 }),
            ("Dict", new Hashtable { ["k"] = Custom(("Z", 3)) }));
        h.Cmdlet.Recurse = true;
        h.Run();

        var ht = h.Only<OrderedDictionary>();
        Assert.Equal(1, Assert.IsType<OrderedDictionary>(ht["Inner"])["X"]);

        var list = Assert.IsType<object?[]>(ht["List"]);
        Assert.Equal(2, Assert.IsType<OrderedDictionary>(list[0])["Y"]);
        Assert.Equal("str", list[1]);
        Assert.Equal(3, list[2]);

        var dict = Assert.IsType<OrderedDictionary>(ht["Dict"]);
        Assert.Equal(3, Assert.IsType<OrderedDictionary>(dict["k"])["Z"]);
    }

    [Fact]
    public void HashtableInputBecomesOrderedDictionary()
    {
        var input = new Hashtable { ["a"] = 1 };
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = PSObject.AsPSObject(input);
        h.Run();
        Assert.Equal(1, h.Only<OrderedDictionary>()["a"]);
    }

    [Fact]
    public void OrderedDictionaryInputPassesThroughWithoutRecurse()
    {
        var input = new OrderedDictionary { ["a"] = 1 };
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = PSObject.AsPSObject(input);
        h.Run();
        Assert.Same(input, Assert.Single(h.Output));
    }

    [Fact]
    public void ScriptPropertiesThatThrowAreSkipped()
    {
        var pso = Custom(("Good", 1));
        pso.Properties.Add(new PSScriptProperty("Bad", ScriptBlock.Create("throw 'boom'")));
        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = pso;
        h.Run();

        var ht = h.Only<OrderedDictionary>();
        Assert.Equal(["Good"], ht.Keys.Cast<string>());
    }

    [Fact]
    public void NotePropertiesAddedToARealObjectAreConvertedWhenRecursing()
    {
        var wrapped = PSObject.AsPSObject(new Uri("https://example.com"));
        wrapped.Properties.Add(new PSNoteProperty("Tag", "x"));

        var h = new CmdletHarness<ConvertToHashTableCmdlet>();
        h.Cmdlet.InputObject = Custom(("Link", (object?)wrapped));
        h.Cmdlet.Recurse = true;
        h.Run();

        var link = Assert.IsType<OrderedDictionary>(h.Only<OrderedDictionary>()["Link"]);
        Assert.Equal("x", link["Tag"]);
        Assert.Equal("example.com", link["Host"]);
    }
}
