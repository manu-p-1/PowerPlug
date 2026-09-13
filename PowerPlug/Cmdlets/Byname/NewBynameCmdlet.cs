using System.Management.Automation;
using PowerPlug.Attributes;

namespace PowerPlug.Cmdlets.Byname;

/// <summary>
/// <para type="synopsis">Creates an alias and persists it to $PROFILE.</para>
/// <para type="description">New-Byname wraps New-Alias. The alias is created in the current session and the
/// equivalent New-Alias line is appended to your $PROFILE so it is available in every new session. If the
/// value is a function defined in the current session, the function body is written to the profile as well.
/// The profile file is created when it does not exist yet.</para>
/// <example>
/// <para>Create a persistent alias for Get-ChildItem</para>
/// <code>New-Byname -Name list -Value Get-ChildItem</code>
/// </example>
/// <example>
/// <para>Persist a session function under a short name</para>
/// <code>function Get-Uptime2 { (Get-Date) - (Get-Process -Id $PID).StartTime }
/// New-Byname up Get-Uptime2 -Description "Session uptime"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "Byname", SupportsShouldProcess = true,
    HelpUri = "https://github.com/manu-p-1/PowerPlug#aliases-byname")]
[Alias("nbn")]
[OutputType(typeof(AliasInfo))]
[ExperimentalCmdlet("It edits your $PROFILE.")]
public sealed class NewBynameCmdlet : WritableBynameCmdlet
{
    /// <inheritdoc />
    protected override string AliasVerb => "New";

    /// <inheritdoc />
    protected override void ProcessRecord() => WriteByname(replaceExisting: false);
}
