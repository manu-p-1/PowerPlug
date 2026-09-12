using System.Management.Automation;
using PowerPlug.Attributes;

namespace PowerPlug.Cmdlets.Profile;

/// <summary>
/// <para type="synopsis">Removes an alias and deletes it from $PROFILE.</para>
/// <para type="description">Remove-Byname wraps Remove-Alias. The alias is removed from the current session and
/// every Byname line for that name is deleted from $PROFILE, together with function definitions those lines
/// pointed at. Use -WhatIf to preview the change.</para>
/// <example>
/// <para>Remove a persisted alias</para>
/// <code>Remove-Byname -Name list</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "Byname", SupportsShouldProcess = true,
    HelpUri = "https://github.com/manu-p-1/PowerPlug#aliases-byname")]
[Alias("rbn")]
[ExperimentalCmdlet("It edits your $PROFILE and removes every entry with this alias name.")]
public sealed class RemoveBynameCmdlet : BynameCmdlet
{
    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        if (!ShouldProcess($"Alias '{Name}'", "Remove-Byname"))
        {
            return;
        }

        var writer = GetProfileWriter();

        InvokeAliasCommand("Remove-Alias", new Dictionary<string, object?>
        {
            ["Name"] = Name,
            ["Scope"] = Scope,
            ["Force"] = Force.IsPresent,
        });

        var removed = writer.Remove(Name);
        if (removed == 0)
        {
            WriteWarning($"No Byname entries for '{Name}' were found in {writer.ProfilePath}.");
        }
        else
        {
            WriteVerbose($"Removed {removed} entr{(removed == 1 ? "y" : "ies")} for '{Name}' from {writer.ProfilePath}.");
        }
    }

    /// <summary>
    /// The Remove-Alias command as it would be typed at the prompt.
    /// </summary>
    public override string ToString() => $"Remove-Alias -Name {Name} -Scope {Scope}{(Force ? " -Force" : string.Empty)}";
}
