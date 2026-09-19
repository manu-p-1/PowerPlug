using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Reports the text encoding and line ending style of files.</para>
/// <para type="description">Looks at the first 64 KB of each file to find a byte order mark, decide whether the content
/// is UTF-8, UTF-16, UTF-32, plain ASCII or binary, and count which line endings are in use.</para>
/// <example>
/// <para>Check one file</para>
/// <code>Get-FileEncoding ./script.ps1</code>
/// </example>
/// <example>
/// <para>Find files in a repo that still use CRLF</para>
/// <code>Get-ChildItem -Recurse -File | Get-FileEncoding | Where-Object LineEnding -ne LF</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "FileEncoding")]
[Alias("gfe")]
[OutputType(typeof(FileEncodingInfo))]
public sealed class GetFileEncodingCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Files to inspect. Wildcards are allowed and items can be piped from Get-ChildItem.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            foreach (var file in ResolvePaths(path))
            {
                if (Directory.Exists(file))
                {
                    WriteVerbose($"Skipped directory: {file}");
                    continue;
                }

                if (!File.Exists(file))
                {
                    WriteFileError(new FileNotFoundException($"File not found: {file}"), file);
                    continue;
                }

                try
                {
                    var inspection = TextFileInspector.Inspect(file);
                    WriteObject(new FileEncodingInfo
                    {
                        Path = file,
                        Encoding = inspection.Encoding,
                        HasBom = inspection.HasBom,
                        LineEnding = inspection.LineEnding,
                        IsBinary = inspection.IsBinary,
                        LineCount = inspection.LineCount,
                    });
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    WriteFileError(ex, file);
                }
            }
        }
    }
}
