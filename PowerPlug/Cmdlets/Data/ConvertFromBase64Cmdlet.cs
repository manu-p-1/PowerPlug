using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Decodes a Base64 string to text or to a file.</para>
/// <para type="description">Decodes standard Base64 or Base64Url input. By default the bytes are returned as text
/// using the chosen encoding. Pass -OutputPath to write the raw bytes to a file instead, which is the right choice
/// for binary content.</para>
/// <example>
/// <para>Decode to text</para>
/// <code>"SGVsbG8sIFdvcmxkIQ==" | ConvertFrom-Base64</code>
/// </example>
/// <example>
/// <para>Decode to a file</para>
/// <code>ConvertFrom-Base64 -InputString $data -OutputPath ./decoded.bin</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "Base64", DefaultParameterSetName = StringSet, SupportsShouldProcess = true)]
[Alias("frombase64")]
[OutputType(typeof(string), ParameterSetName = [StringSet])]
[OutputType(typeof(FileInfo), ParameterSetName = [FileSet])]
public sealed class ConvertFromBase64Cmdlet : PowerPlugCmdlet
{
    private const string StringSet = "String";
    private const string FileSet = "File";

    /// <summary>
    /// <para type="description">The Base64 or Base64Url text to decode.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public string InputString { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Write the decoded bytes to this file instead of returning text.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = FileSet)]
    [ValidateNotNullOrEmpty]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Text encoding used to read the decoded bytes. Defaults to UTF8.</para>
    /// </summary>
    [Parameter(ParameterSetName = StringSet)]
    [ValidateSet(TextEncodings.Utf8, TextEncodings.Ascii, TextEncodings.Unicode, TextEncodings.Utf32, TextEncodings.Latin1)]
    public string Encoding { get; set; } = TextEncodings.Utf8;

    /// <summary>
    /// <para type="description">Overwrite the output file if it exists.</para>
    /// </summary>
    [Parameter(ParameterSetName = FileSet)]
    public SwitchParameter Force { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        byte[] bytes;
        try
        {
            bytes = Base64Url.DecodeFlexible(InputString);
        }
        catch (FormatException ex)
        {
            WriteError(ex, "InvalidBase64", ErrorCategory.InvalidData, InputString);
            return;
        }

        if (ParameterSetName == StringSet)
        {
            WriteObject(TextEncodings.Parse(Encoding).GetString(bytes));
            return;
        }

        var resolved = ResolvePath(OutputPath);
        if (File.Exists(resolved) && !Force)
        {
            WriteError(new IOException($"File already exists: {resolved}. Use -Force to overwrite."),
                "FileExists", ErrorCategory.ResourceExists, resolved);
            return;
        }

        if (!ShouldProcess(resolved, "Write decoded bytes"))
        {
            return;
        }

        try
        {
            File.WriteAllBytes(resolved, bytes);
            WriteObject(new FileInfo(resolved));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, resolved, writing: true);
        }
    }
}
