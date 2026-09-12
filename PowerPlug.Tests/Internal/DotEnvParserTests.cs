using PowerPlug.Internal;

namespace PowerPlug.Tests.Internal;

public class DotEnvParserTests
{
    [Fact]
    public void Parse_BasicPairsAndComments()
    {
        const string content = """
            # comment
            KEY=value
            OTHER = spaced

            export EXPORTED=yes
            """;

        var pairs = DotEnvParser.Parse(content);

        Assert.Equal(
            [("KEY", "value"), ("OTHER", "spaced"), ("EXPORTED", "yes")],
            pairs.Select(p => (p.Key, p.Value)).ToArray());
    }

    [Fact]
    public void Parse_InlineCommentOnUnquotedValue()
    {
        var pairs = DotEnvParser.Parse("A=hello # trailing\nB=no#space");
        Assert.Equal("hello", pairs[0].Value);
        Assert.Equal("no#space", pairs[1].Value);
    }

    [Fact]
    public void Parse_DoubleQuotedValuesUnescape()
    {
        var pairs = DotEnvParser.Parse("A=\"line1\\nline2 \\\"quoted\\\" \\$x\"");
        Assert.Equal("line1\nline2 \"quoted\" $x", pairs[0].Value);
    }

    [Fact]
    public void Parse_SingleQuotedValuesAreLiteral()
    {
        var pairs = DotEnvParser.Parse("A='raw \\n $HOME # not a comment'");
        Assert.Equal("raw \\n $HOME # not a comment", pairs[0].Value);
    }

    [Fact]
    public void Parse_MultiLineQuotedValue()
    {
        var pairs = DotEnvParser.Parse("KEY=\"first\nsecond\nthird\"\nNEXT=1");
        Assert.Equal("first\nsecond\nthird", pairs[0].Value);
        Assert.Equal("NEXT", pairs[1].Key);
    }

    [Fact]
    public void Parse_EmptyValue()
    {
        Assert.Equal(string.Empty, DotEnvParser.Parse("EMPTY=")[0].Value);
        Assert.Equal(string.Empty, DotEnvParser.Parse("EMPTY=\"\"")[0].Value);
    }

    [Fact]
    public void Parse_LaterKeyWins()
    {
        var pairs = DotEnvParser.Parse("A=1\nA=2");
        Assert.Single(pairs);
        Assert.Equal("2", pairs[0].Value);
    }

    [Fact]
    public void Parse_ExpandsEarlierKeysAndEnvironment()
    {
        Environment.SetEnvironmentVariable("POWERPLUG_TEST_VAR", "env");
        try
        {
            var pairs = DotEnvParser.Parse("BASE=/opt\nFULL=${BASE}/bin:$POWERPLUG_TEST_VAR\nLIT='${BASE}'", expand: true);
            Assert.Equal("/opt/bin:env", pairs[1].Value);
            Assert.Equal("${BASE}", pairs[2].Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("POWERPLUG_TEST_VAR", null);
        }
    }

    [Fact]
    public void Parse_EscapedDollarStaysLiteralWhenExpanding()
    {
        var pairs = DotEnvParser.Parse("A=\"cost \\$HOME\"", expand: true);
        Assert.Equal("cost $HOME", pairs[0].Value);
    }

    [Fact]
    public void Parse_UnknownExpansionBecomesEmpty()
    {
        var pairs = DotEnvParser.Parse("A=${DOES_NOT_EXIST_XYZ}!", expand: true);
        Assert.Equal("!", pairs[0].Value);
    }

    [Theory]
    [InlineData("no equals sign")]
    [InlineData("=value")]
    [InlineData("1BAD=value")]
    [InlineData("BAD.KEY=value")]
    [InlineData("A=\"unterminated")]
    public void Parse_RejectsMalformedLines(string content)
    {
        Assert.Throws<FormatException>(() => DotEnvParser.Parse(content));
    }

    [Fact]
    public void Parse_HandlesWindowsLineEndings()
    {
        var pairs = DotEnvParser.Parse("A=1\r\nB=2\r\n");
        Assert.Equal(2, pairs.Count);
        Assert.Equal("1", pairs[0].Value);
    }
}
