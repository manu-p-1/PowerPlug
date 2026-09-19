using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.Security;

/// <summary>
/// <para type="synopsis">Tests whether the current session is running elevated.</para>
/// <para type="description">Returns true when PowerShell is running as an administrator (Windows) or as root
/// (macOS and Linux).</para>
/// <example>
/// <para>Guard a script</para>
/// <code>if (-not (Test-Elevation)) { throw "Run this script as administrator." }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "Elevation")]
[Alias("isadmin", "Test-Administrator")]
[OutputType(typeof(bool))]
public sealed class TestElevationCmdlet : PowerPlugCmdlet
{
    /// <inheritdoc />
    protected override void ProcessRecord() => WriteObject(Elevation.IsElevated());
}
