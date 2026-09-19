using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Converts text files between LF and CRLF line endings.</para>
/// <para type="description">Rewrites line endings in place while leaving every other byte, the encoding and any byte
/// order mark exactly as they were. Binary files are skipped. Each file is written to a temporary name and swapped
/// in, so an interrupted run never leaves a half written file. Supports -WhatIf.</para>
/// <example>
/// <para>Normalise a repository to LF</para>
/// <code>Get-ChildItem -Recurse -File -Include *.cs,*.ps1,*.md | Convert-LineEnding -To LF</code>
/// </example>
/// <example>
/// <para>Preview what would change</para>
/// <code>Convert-LineEnding *.txt -To CRLF -WhatIf -PassThru</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Convert, "LineEnding", SupportsShouldProcess = true)]
[Alias("eol")]
[OutputType(typeof(LineEndingResult))]
public sealed class ConvertLineEndingCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Files to convert. Wildcards are allowed and items can be piped from Get-ChildItem.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <summary>
    /// <para type="description">Target line ending: LF or CRLF.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    [ValidateSet("LF", "CRLF", IgnoreCase = true)]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Return a result for every file, including ones that were skipped.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        To = To.ToUpperInvariant();
    }

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

                var result = ConvertOne(file);
                if (result is not null && (PassThru || result.Changed))
                {
                    WriteObject(result);
                }
            }
        }
    }

    private LineEndingResult? ConvertOne(string file)
    {
        byte[] content;
        TextFileInspector.Inspection inspection;
        try
        {
            content = File.ReadAllBytes(file);
            inspection = TextFileInspector.Inspect(content.AsSpan(0, Math.Min(content.Length, TextFileInspector.SampleSize)), content.Length > TextFileInspector.SampleSize);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, file);
            return null;
        }

        if (inspection.IsBinary)
        {
            return new LineEndingResult { Path = file, From = "None", To = To, Changed = false, Reason = "Binary file" };
        }

        var converted = TextFileInspector.ConvertLineEndings(content, inspection, To);
        if (ReferenceEquals(converted, content))
        {
            return new LineEndingResult { Path = file, From = inspection.LineEnding, To = To, Changed = false, Reason = "Already " + To };
        }

        if (!ShouldProcess(file, $"Convert line endings to {To}"))
        {
            return new LineEndingResult { Path = file, From = inspection.LineEnding, To = To, Changed = false, Reason = "Preview" };
        }

        try
        {
            AtomicFile.Replace(file, converted);
            return new LineEndingResult { Path = file, From = inspection.LineEnding, To = To, Changed = true };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, file, writing: true);
            return null;
        }
    }
}
