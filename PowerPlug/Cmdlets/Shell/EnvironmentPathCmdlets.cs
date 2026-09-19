using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Shell;

/// <summary>
/// Shared parameters and helpers for the PATH cmdlets.
/// </summary>
public abstract class EnvironmentPathCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Which PATH to work with: Process (this session), User or Machine. User and Machine
    /// are Windows only. Defaults to Process.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("Process", "User", "Machine")]
    public string Target { get; set; } = "Process";

    /// <summary>
    /// Resolves the target, warning and falling back to Process when the platform cannot persist it.
    /// </summary>
    protected EnvironmentVariableTarget ResolveTarget()
    {
        var target = PathEnvironment.ParseTarget(Target);
        if (!PathEnvironment.TargetSupported(target))
        {
            WriteWarning($"The '{Target}' target is only available on Windows. Using 'Process' instead.");
            Target = "Process";
            return EnvironmentVariableTarget.Process;
        }

        return target;
    }

    /// <summary>
    /// Reads PATH entries for the target.
    /// </summary>
    protected static List<string> ReadEntries(EnvironmentVariableTarget target) =>
        PathEnvironment.Split(Environment.GetEnvironmentVariable("PATH", target));

    /// <summary>
    /// Writes PATH entries for the target. Process changes also update $env:PATH in the session.
    /// </summary>
    protected static void WriteEntries(EnvironmentVariableTarget target, IEnumerable<string> entries) =>
        Environment.SetEnvironmentVariable("PATH", PathEnvironment.Join(entries), target);

    /// <summary>
    /// Builds output rows for a list of entries, flagging missing directories and duplicates.
    /// </summary>
    protected IEnumerable<PathEntry> Describe(IReadOnlyList<string> entries)
    {
        var seen = new HashSet<string>(PathEnvironment.Comparer);
        for (var i = 0; i < entries.Count; i++)
        {
            var normalized = PathEnvironment.Normalize(entries[i]);
            yield return new PathEntry
            {
                Index = i,
                Path = entries[i],
                Exists = Directory.Exists(normalized),
                Duplicate = !seen.Add(normalized),
                Target = Target,
            };
        }
    }
}

/// <summary>
/// <para type="synopsis">Lists the entries in the PATH environment variable.</para>
/// <para type="description">Splits PATH into one row per directory and flags entries that do not exist or appear
/// more than once. Useful when a tool cannot be found or PATH has grown out of control.</para>
/// <example>
/// <para>Show PATH</para>
/// <code>Get-EnvironmentPath</code>
/// </example>
/// <example>
/// <para>Show problems only</para>
/// <code>Get-EnvironmentPath | Where-Object { -not $_.Exists -or $_.Duplicate }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "EnvironmentPath")]
[Alias("gpath")]
[OutputType(typeof(PathEntry))]
public sealed class GetEnvironmentPathCmdlet : EnvironmentPathCmdlet
{
    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var entries = ReadEntries(ResolveTarget());
        if (entries.Count == 0)
        {
            WriteWarning("PATH is empty.");
            return;
        }

        foreach (var entry in Describe(entries))
        {
            WriteObject(entry);
        }
    }
}

/// <summary>
/// <para type="synopsis">Adds directories to PATH.</para>
/// <para type="description">Appends (or with -Prepend, inserts at the front) one or more directories to PATH,
/// skipping any that are already present. Directories that do not exist are rejected unless -Force is used.
/// Changes to the Process target take effect in the current session immediately; User and Machine changes
/// (Windows only) apply to new processes.</para>
/// <example>
/// <para>Add a tools folder for this session</para>
/// <code>Add-EnvironmentPath ~/tools</code>
/// </example>
/// <example>
/// <para>Put a folder first in the user PATH on Windows</para>
/// <code>Add-EnvironmentPath C:\bin -Target User -Prepend</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Add, "EnvironmentPath", SupportsShouldProcess = true)]
[Alias("addpath")]
[OutputType(typeof(PathEntry))]
[Attributes.ExperimentalCmdlet("It changes environment variables. User and Machine targets edit the registry on Windows.")]
public sealed class AddEnvironmentPathCmdlet : EnvironmentPathCmdlet
{
    /// <summary>
    /// <para type="description">Directories to add.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <summary>
    /// <para type="description">Insert at the front of PATH instead of the end.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Prepend { get; set; }

    /// <summary>
    /// <para type="description">Add the directory even if it does not exist yet.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// <para type="description">Return the resulting PATH entries.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var target = ResolveTarget();
        var entries = ReadEntries(target);
        var changed = false;

        foreach (var raw in Path)
        {
            var resolved = ResolvePath(raw);
            if (!Directory.Exists(resolved) && !Force)
            {
                WriteError(new DirectoryNotFoundException($"Directory not found: {resolved}. Use -Force to add it anyway."),
                    "DirectoryNotFound", ErrorCategory.ObjectNotFound, resolved);
                continue;
            }

            if (entries.Any(e => PathEnvironment.Same(e, resolved)))
            {
                WriteVerbose($"Already in PATH: {resolved}");
                continue;
            }

            if (!ShouldProcess($"{Target} PATH", $"Add '{resolved}'"))
            {
                continue;
            }

            if (Prepend)
            {
                entries.Insert(0, resolved);
            }
            else
            {
                entries.Add(resolved);
            }

            changed = true;
        }

        if (changed)
        {
            try
            {
                WriteEntries(target, entries);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, Target));
            }
        }

        if (PassThru)
        {
            foreach (var entry in Describe(entries))
            {
                WriteObject(entry);
            }
        }
    }
}

/// <summary>
/// <para type="synopsis">Removes directories from PATH.</para>
/// <para type="description">Removes the given directories from PATH. -RemoveMissing drops every entry whose
/// directory no longer exists and -RemoveDuplicates keeps only the first occurrence of each directory, which
/// together make a quick PATH clean up. Supports -WhatIf.</para>
/// <example>
/// <para>Remove one entry</para>
/// <code>Remove-EnvironmentPath ~/old-tools</code>
/// </example>
/// <example>
/// <para>Clean up the user PATH on Windows</para>
/// <code>Remove-EnvironmentPath -Target User -RemoveMissing -RemoveDuplicates -WhatIf</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "EnvironmentPath", SupportsShouldProcess = true)]
[Alias("rmpath")]
[OutputType(typeof(PathEntry))]
[Attributes.ExperimentalCmdlet("It changes environment variables. User and Machine targets edit the registry on Windows.")]
public sealed class RemoveEnvironmentPathCmdlet : EnvironmentPathCmdlet
{
    /// <summary>
    /// <para type="description">Directories to remove.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    public string[]? Path { get; set; }

    /// <summary>
    /// <para type="description">Remove entries whose directory does not exist.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter RemoveMissing { get; set; }

    /// <summary>
    /// <para type="description">Remove repeated entries, keeping the first.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter RemoveDuplicates { get; set; }

    /// <summary>
    /// <para type="description">Return the resulting PATH entries.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        if (Path is not { Length: > 0 } && !RemoveMissing && !RemoveDuplicates)
        {
            ThrowTerminatingError(new ErrorRecord(
                new PSArgumentException("Specify -Path, -RemoveMissing or -RemoveDuplicates."),
                "NothingToRemove", ErrorCategory.InvalidArgument, null));
        }

        var target = ResolveTarget();
        var entries = ReadEntries(target);
        var kept = new List<string>(entries.Count);
        var seen = new HashSet<string>(PathEnvironment.Comparer);
        var wanted = (Path ?? []).Select(ResolvePath).ToArray();

        foreach (var entry in entries)
        {
            var normalized = PathEnvironment.Normalize(entry);
            string? reason = null;

            if (wanted.Any(w => PathEnvironment.Same(w, entry)))
            {
                reason = "requested";
            }
            else if (RemoveDuplicates && seen.Contains(normalized))
            {
                reason = "duplicate";
            }
            else if (RemoveMissing && !Directory.Exists(normalized))
            {
                reason = "missing";
            }

            if (reason is not null && ShouldProcess($"{Target} PATH", $"Remove '{entry}' ({reason})"))
            {
                continue;
            }

            seen.Add(normalized);
            kept.Add(entry);
        }

        if (kept.Count != entries.Count)
        {
            try
            {
                WriteEntries(target, kept);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, Target));
            }

            WriteVerbose($"Removed {entries.Count - kept.Count} entr{(entries.Count - kept.Count == 1 ? "y" : "ies")} from {Target} PATH.");
        }

        if (PassThru)
        {
            foreach (var entry in Describe(kept))
            {
                WriteObject(entry);
            }
        }
    }
}
