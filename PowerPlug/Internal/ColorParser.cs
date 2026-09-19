using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using PowerPlug.Models;

namespace PowerPlug.Internal;

/// <summary>
/// Parses colour notations (hex, rgb(), hsl(), CSS names) and produces a <see cref="ColorInfo"/>.
/// </summary>
internal static class ColorParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex RgbPattern = new(
        @"^rgba?\(\s*(?<r>\d{1,3})\s*[, ]\s*(?<g>\d{1,3})\s*[, ]\s*(?<b>\d{1,3})\s*(?:[,/]\s*(?<a>[\d.]+%?)\s*)?\)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex HslPattern = new(
        @"^hsla?\(\s*(?<h>[\d.]+)(?:deg)?\s*[, ]\s*(?<s>[\d.]+)%\s*[, ]\s*(?<l>[\d.]+)%\s*(?:[,/]\s*(?<a>[\d.]+%?)\s*)?\)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>
    /// Parses "#RGB", "#RGBA", "#RRGGBB", "#RRGGBBAA", "rgb(r, g, b)", "rgba(r, g, b, a)", "hsl(h, s%, l%)"
    /// or a known colour name such as "Tomato".
    /// </summary>
    /// <exception cref="FormatException">The text is not a recognised colour.</exception>
    public static ColorInfo Parse(string text)
    {
        var value = text.Trim();

        if (value.StartsWith('#') || IsHexOnly(value))
        {
            return FromHex(value.TrimStart('#'));
        }

        var rgb = RgbPattern.Match(value);
        if (rgb.Success)
        {
            return FromRgb(
                ParseChannel(rgb.Groups["r"].Value),
                ParseChannel(rgb.Groups["g"].Value),
                ParseChannel(rgb.Groups["b"].Value),
                ParseAlpha(rgb.Groups["a"]));
        }

        var hsl = HslPattern.Match(value);
        if (hsl.Success)
        {
            return FromHsl(
                double.Parse(hsl.Groups["h"].Value, CultureInfo.InvariantCulture),
                double.Parse(hsl.Groups["s"].Value, CultureInfo.InvariantCulture),
                double.Parse(hsl.Groups["l"].Value, CultureInfo.InvariantCulture),
                ParseAlpha(hsl.Groups["a"]));
        }

        var named = Color.FromName(value);
        if (named.IsKnownColor && !named.IsSystemColor)
        {
            return FromRgb(named.R, named.G, named.B, named.A);
        }

        throw new FormatException($"'{text}' is not a recognised colour. Use #RRGGBB, rgb(r,g,b), hsl(h,s%,l%) or a CSS colour name.");
    }

    /// <summary>
    /// Builds a <see cref="ColorInfo"/> from RGB(A) channels.
    /// </summary>
    public static ColorInfo FromRgb(int r, int g, int b, int a = 255)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        a = Math.Clamp(a, 0, 255);

        var (h, s, l) = ToHsl(r, g, b);
        var alphaText = (a / 255.0).ToString("0.##", CultureInfo.InvariantCulture);

        return new ColorInfo
        {
            Hex = a == 255
                ? $"#{r:X2}{g:X2}{b:X2}"
                : $"#{r:X2}{g:X2}{b:X2}{a:X2}",
            R = r,
            G = g,
            B = b,
            A = a,
            Rgb = a == 255
                ? $"rgb({r}, {g}, {b})"
                : string.Create(CultureInfo.InvariantCulture, $"rgba({r}, {g}, {b}, {alphaText})"),
            H = Math.Round(h, 1),
            S = Math.Round(s, 1),
            L = Math.Round(l, 1),
            Hsl = string.Create(CultureInfo.InvariantCulture, $"hsl({Math.Round(h)}, {Math.Round(s)}%, {Math.Round(l)}%)"),
        };
    }

    /// <summary>
    /// Builds a <see cref="ColorInfo"/> from hue (0-360), saturation (0-100) and lightness (0-100).
    /// </summary>
    public static ColorInfo FromHsl(double h, double s, double l, int a = 255)
    {
        h = ((h % 360) + 360) % 360;
        s = Math.Clamp(s, 0, 100) / 100.0;
        l = Math.Clamp(l, 0, 100) / 100.0;

        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = l - c / 2;

        var (r1, g1, b1) = (int)(h / 60) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return FromRgb(
            (int)Math.Round((r1 + m) * 255),
            (int)Math.Round((g1 + m) * 255),
            (int)Math.Round((b1 + m) * 255),
            a);
    }

    private static ColorInfo FromHex(string hex)
    {
        if (!hex.All(Uri.IsHexDigit))
        {
            throw new FormatException($"'#{hex}' contains characters that are not hex digits.");
        }

        switch (hex.Length)
        {
            case 3:
            case 4:
                hex = string.Concat(hex.Select(ch => new string(ch, 2)));
                break;
            case 6:
            case 8:
                break;
            default:
                throw new FormatException($"'#{hex}' must have 3, 4, 6 or 8 hex digits.");
        }

        var r = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var a = hex.Length == 8 ? int.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) : 255;
        return FromRgb(r, g, b, a);
    }

    private static (double H, double S, double L) ToHsl(int r, int g, int b)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;
        var l = (max + min) / 2;

        if (delta == 0)
        {
            return (0, 0, l * 100);
        }

        var s = delta / (1 - Math.Abs(2 * l - 1));
        var h = max switch
        {
            _ when max == rf => 60 * (((gf - bf) / delta) % 6),
            _ when max == gf => 60 * ((bf - rf) / delta + 2),
            _ => 60 * ((rf - gf) / delta + 4),
        };

        if (h < 0)
        {
            h += 360;
        }

        return (h, s * 100, l * 100);
    }

    private static bool IsHexOnly(string value) => value.Length is 3 or 4 or 6 or 8 && value.All(Uri.IsHexDigit);

    private static int ParseChannel(string text) => Math.Clamp(int.Parse(text, CultureInfo.InvariantCulture), 0, 255);

    private static int ParseAlpha(Group group)
    {
        if (!group.Success)
        {
            return 255;
        }

        var text = group.Value;
        if (text.EndsWith('%'))
        {
            return (int)Math.Round(double.Parse(text.TrimEnd('%'), CultureInfo.InvariantCulture) / 100 * 255);
        }

        var alpha = double.Parse(text, CultureInfo.InvariantCulture);
        return (int)Math.Round(Math.Clamp(alpha, 0, 1) * 255);
    }
}
