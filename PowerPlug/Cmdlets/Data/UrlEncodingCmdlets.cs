using System.Management.Automation;
using System.Net;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Percent-encodes a string for use in a URL.</para>
/// <para type="description">Escapes characters that are not allowed in a URL component. The default follows
/// RFC 3986 (spaces become %20). Use -Form for application/x-www-form-urlencoded output where spaces become
/// plus signs, which is what HTML forms and many query string parsers expect.</para>
/// <example>
/// <para>Encode a query value</para>
/// <code>"name=John Doe&amp;city=Zürich" | ConvertTo-UrlEncoding</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertTo, "UrlEncoding")]
[Alias("urlencode")]
[OutputType(typeof(string))]
public sealed class ConvertToUrlEncodingCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The text to encode.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [AllowEmptyString]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Use form encoding (spaces as plus signs) instead of RFC 3986 encoding.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Form { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() =>
        WriteObject(Form ? WebUtility.UrlEncode(InputString) : Uri.EscapeDataString(InputString));
}

/// <summary>
/// <para type="synopsis">Decodes a percent-encoded URL string.</para>
/// <para type="description">Reverses percent-encoding. Plus signs are left alone unless -Form is specified,
/// in which case they are treated as spaces per application/x-www-form-urlencoded rules.</para>
/// <example>
/// <para>Decode a query string</para>
/// <code>"name%3DJohn%20Doe" | ConvertFrom-UrlEncoding</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "UrlEncoding")]
[Alias("urldecode")]
[OutputType(typeof(string))]
public sealed class ConvertFromUrlEncodingCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The encoded text.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [AllowEmptyString]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Treat plus signs as spaces.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Form { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() =>
        WriteObject(Form ? WebUtility.UrlDecode(InputString) : Uri.UnescapeDataString(InputString));
}
