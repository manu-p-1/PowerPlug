using System.Management.Automation;
using PowerPlug.Attributes;

namespace PowerPlug.Cmdlets.Byname;

/// <summary>
/// <para type="synopsis">Changes an alias and updates the persisted copy in $PROFILE.</para>
/// <para type="description">Set-Byname wraps Set-Alias. The alias is updated in the current session, any
/// previous Byname lines for the same name are removed from $PROFILE, and the new Set-Alias line is appended.
/// Functions the alias points at are persisted as with New-Byname.</para>
/// <example>
/// <para>Repoint an existing Byname</para>
/// <code>Set-Byname -Name gh -Value Get-Help</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Set, "Byname", SupportsShouldProcess = true,
    HelpUri = "https://github.com/manu-p-1/PowerPlug#aliases-byname")]
[Alias("sbn")]
[OutputType(typeof(AliasInfo))]
[ExperimentalCmdlet("It edits your $PROFILE.")]
public sealed class SetBynameCmdlet : WritableBynameCmdlet
{
    /// <inheritdoc />
    protected override string AliasVerb => "Set";

    /// <inheritdoc />
    protected override void ProcessRecord() => WriteByname(replaceExisting: true);
}
