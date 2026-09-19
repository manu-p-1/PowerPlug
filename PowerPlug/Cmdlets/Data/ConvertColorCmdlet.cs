using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Converts a colour between hex, RGB and HSL notations.</para>
/// <para type="description">Accepts a colour as text (#RRGGBB, #RGB, rgb(), rgba(), hsl(), or a CSS colour name like
/// "Tomato") or as separate channel values, and returns every notation at once.</para>
/// <example>
/// <para>Convert a hex colour</para>
/// <code>Convert-Color "#FF8800"</code>
/// </example>
/// <example>
/// <para>Convert from channels</para>
/// <code>Convert-Color -R 255 -G 136 -B 0</code>
/// </example>
/// <example>
/// <para>Convert from HSL</para>
/// <code>Convert-Color -H 32 -S 100 -L 50</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Convert, "Color", DefaultParameterSetName = TextSet)]
[Alias("color")]
[OutputType(typeof(ColorInfo))]
public sealed class ConvertColorCmdlet : PowerPlugCmdlet
{
    private const string TextSet = "Text";
    private const string RgbSet = "Rgb";
    private const string HslSet = "Hsl";

    /// <summary>
    /// <para type="description">The colour in any supported text notation.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = TextSet)]
    [ValidateNotNullOrEmpty]
    [Alias("Color", "Hex")]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Red channel, 0-255.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = RgbSet)]
    [ValidateRange(0, 255)]
    public int R { get; set; }

    /// <summary>
    /// <para type="description">Green channel, 0-255.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = RgbSet)]
    [ValidateRange(0, 255)]
    public int G { get; set; }

    /// <summary>
    /// <para type="description">Blue channel, 0-255.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = RgbSet)]
    [ValidateRange(0, 255)]
    public int B { get; set; }

    /// <summary>
    /// <para type="description">Hue in degrees, 0-360.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = HslSet)]
    [ValidateRange(0, 360)]
    public double H { get; set; }

    /// <summary>
    /// <para type="description">Saturation percentage, 0-100.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = HslSet)]
    [ValidateRange(0, 100)]
    public double S { get; set; }

    /// <summary>
    /// <para type="description">Lightness percentage, 0-100.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = HslSet)]
    [ValidateRange(0, 100)]
    public double L { get; set; }

    /// <summary>
    /// <para type="description">Alpha channel, 0-255. Defaults to 255 (opaque).</para>
    /// </summary>
    [Parameter(ParameterSetName = RgbSet)]
    [Parameter(ParameterSetName = HslSet)]
    [ValidateRange(0, 255)]
    public int A { get; set; } = 255;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        try
        {
            WriteObject(ParameterSetName switch
            {
                RgbSet => ColorParser.FromRgb(R, G, B, A),
                HslSet => ColorParser.FromHsl(H, S, L, A),
                _ => ColorParser.Parse(InputString),
            });
        }
        catch (FormatException ex)
        {
            WriteError(ex, "InvalidColor", ErrorCategory.InvalidArgument, InputString);
        }
    }
}
