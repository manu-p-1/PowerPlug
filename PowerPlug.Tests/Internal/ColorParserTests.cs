using PowerPlug.Internal;

namespace PowerPlug.Tests.Internal;

public class ColorParserTests
{
    [Theory]
    [InlineData("#FF8800", 255, 136, 0)]
    [InlineData("ff8800", 255, 136, 0)]
    [InlineData("#F80", 255, 136, 0)]
    [InlineData("rgb(255, 136, 0)", 255, 136, 0)]
    [InlineData("rgb(255 136 0)", 255, 136, 0)]
    [InlineData("hsl(32, 100%, 50%)", 255, 136, 0)]
    [InlineData("Tomato", 255, 99, 71)]
    [InlineData("  white ", 255, 255, 255)]
    public void Parse_ManyNotations(string input, int r, int g, int b)
    {
        var color = ColorParser.Parse(input);
        Assert.Equal((r, g, b, 255), (color.R, color.G, color.B, color.A));
    }

    [Theory]
    [InlineData("#FF880080", 128)]
    [InlineData("rgba(255, 136, 0, 0.5)", 128)]
    [InlineData("rgba(255, 136, 0, 50%)", 128)]
    [InlineData("hsla(32, 100%, 50%, 0.5)", 128)]
    public void Parse_Alpha(string input, int alpha)
    {
        Assert.Equal(alpha, ColorParser.Parse(input).A);
    }

    [Fact]
    public void FromRgb_ProducesAllNotations()
    {
        var color = ColorParser.FromRgb(255, 136, 0);
        Assert.Equal("#FF8800", color.Hex);
        Assert.Equal("rgb(255, 136, 0)", color.Rgb);
        Assert.Equal("hsl(32, 100%, 50%)", color.Hsl);
        Assert.Equal(32, color.H);
        Assert.Equal(100, color.S);
        Assert.Equal(50, color.L);
    }

    [Fact]
    public void FromRgb_WithAlphaUsesEightDigitHexAndRgba()
    {
        var color = ColorParser.FromRgb(0, 0, 0, 128);
        Assert.Equal("#00000080", color.Hex);
        Assert.Equal("rgba(0, 0, 0, 0.5)", color.Rgb);
    }

    [Fact]
    public void FromRgb_ClampsOutOfRangeChannels()
    {
        var color = ColorParser.FromRgb(300, -5, 128);
        Assert.Equal((255, 0, 128), (color.R, color.G, color.B));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 100, 255, 255, 255)]
    [InlineData(0, 100, 50, 255, 0, 0)]
    [InlineData(120, 100, 50, 0, 255, 0)]
    [InlineData(240, 100, 50, 0, 0, 255)]
    [InlineData(360, 100, 50, 255, 0, 0)]
    public void FromHsl_PrimaryColours(double h, double s, double l, int r, int g, int b)
    {
        var color = ColorParser.FromHsl(h, s, l);
        Assert.Equal((r, g, b), (color.R, color.G, color.B));
    }

    [Fact]
    public void RoundTrip_RgbToHslToRgb()
    {
        var original = ColorParser.FromRgb(37, 99, 235);
        var back = ColorParser.FromHsl(original.H, original.S, original.L);
        Assert.InRange(Math.Abs(back.R - original.R), 0, 1);
        Assert.InRange(Math.Abs(back.G - original.G), 0, 1);
        Assert.InRange(Math.Abs(back.B - original.B), 0, 1);
    }

    [Theory]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("rgb(1,2)")]
    [InlineData("notacolour")]
    [InlineData("")]
    public void Parse_RejectsInvalid(string input)
    {
        Assert.Throws<FormatException>(() => ColorParser.Parse(input));
    }
}
