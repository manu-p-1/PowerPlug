using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Encodes a string or a file to Base64.</para>
/// <para type="description">Encodes plain text (from a parameter or the pipeline) or the raw bytes of a file as a
/// Base64 string. Use -UrlSafe to produce the Base64Url alphabet used by JWTs and web APIs.</para>
/// <example>
/// <para>Encode a string</para>
/// <code>"Hello, World!" | ConvertTo-Base64</code>
/// </example>
/// <example>
/// <para>Encode a file</para>
/// <code>ConvertTo-Base64 -Path ./logo.png</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertTo, "Base64", DefaultParameterSetName = StringSet)]
[Alias("tobase64")]
[OutputType(typeof(string))]
public sealed class ConvertToBase64Cmdlet : PowerPlugCmdlet
{
    private const string StringSet = "String";
    private const string FileSet = "File";

    /// <summary>
    /// <para type="description">The text to encode.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = StringSet)]
    [AllowEmptyString]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">A file whose bytes should be encoded.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = FileSet)]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Text encoding used to turn the input string into bytes. Defaults to UTF8.</para>
    /// </summary>
    [Parameter(ParameterSetName = StringSet)]
    [ValidateSet(TextEncodings.Utf8, TextEncodings.Ascii, TextEncodings.Unicode, TextEncodings.Utf32, TextEncodings.Latin1)]
    public string Encoding { get; set; } = TextEncodings.Utf8;

    /// <summary>
    /// <para type="description">Emit Base64Url (dash and underscore instead of plus and slash, no padding).</para>
    /// </summary>
    [Parameter]
    public SwitchParameter UrlSafe { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        byte[] bytes;

        if (ParameterSetName == FileSet)
        {
            var resolved = ResolvePath(Path);
            try
            {
                bytes = File.ReadAllBytes(resolved);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WriteFileError(ex, resolved);
                return;
            }
        }
        else
        {
            bytes = TextEncodings.Parse(Encoding).GetBytes(InputString);
        }

        WriteObject(UrlSafe ? Base64Url.Encode(bytes) : Convert.ToBase64String(bytes));
    }
}
