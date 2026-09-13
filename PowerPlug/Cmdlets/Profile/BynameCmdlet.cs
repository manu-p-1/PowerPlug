using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.Profile;

//It all started from here - this very cmdlet - my baby.

/// <summary>
/// Shared plumbing for the Byname cmdlets: locating $PROFILE and running the underlying alias cmdlet
/// in the caller's runspace.
/// </summary>
public abstract class BynameCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The alias name.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The scope the alias is created or removed in.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("Global", "Local", "Private", "Script")]
    public string Scope { get; set; } = "Local";

    /// <summary>
    /// <para type="description">Allows an existing read-only alias with the same name to be replaced or removed.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Returns a writer for the current user's $PROFILE, or terminates the cmdlet when $PROFILE is not defined.
    /// </summary>
    private protected ProfileAliasWriter GetProfileWriter()
    {
        var profile = SessionState.PSVariable.GetValue("PROFILE");
        var path = profile switch
        {
            PSObject pso => pso.BaseObject?.ToString(),
            null => null,
            _ => profile.ToString(),
        };

        if (string.IsNullOrWhiteSpace(path))
        {
            ThrowTerminatingError(new ErrorRecord(
                new SessionStateException("$PROFILE is not defined in this session, so the Byname cannot be persisted."),
                "ProfileNotDefined", ErrorCategory.ObjectNotFound, null));
        }

        return new ProfileAliasWriter(path!);
    }

    /// <summary>
    /// Runs a built in alias cmdlet in the current runspace and forwards its output. The first error from
    /// the nested command terminates this cmdlet.
    /// </summary>
    private protected void InvokeAliasCommand(string commandName, IReadOnlyDictionary<string, object?> parameters)
    {
        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
        ps.AddCommand(commandName);
        foreach (var (key, value) in parameters)
        {
            ps.AddParameter(key, value);
        }

        var results = ps.Invoke();
        if (ps.Streams.Error.Count > 0)
        {
            ThrowTerminatingError(ps.Streams.Error[0]);
        }

        foreach (var result in results)
        {
            WriteObject(result);
        }
    }
}

/// <summary>
/// Shared parameters for Bynames that write an alias (New-Byname and Set-Byname).
/// </summary>
public abstract class WritableBynameCmdlet : BynameCmdlet
{
    /// <summary>
    /// <para type="description">The command, function, script or executable the alias points at.</para>
    /// </summary>
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">An optional description stored with the alias.</para>
    /// </summary>
    [Parameter]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Alias options such as ReadOnly or Constant.</para>
    /// </summary>
    [Parameter]
    public ScopedItemOptions Option { get; set; } = ScopedItemOptions.None;

    /// <summary>
    /// <para type="description">Returns the created alias to the pipeline.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <summary>
    /// The alias verb this cmdlet wraps: "New" or "Set".
    /// </summary>
    protected abstract string AliasVerb { get; }

    /// <summary>
    /// Runs the wrapped alias cmdlet and then persists the alias, plus any session function it points at,
    /// to $PROFILE. Set-Byname removes previous entries for the name first.
    /// </summary>
    private protected void WriteByname(bool replaceExisting)
    {
        if (!ShouldProcess($"Alias '{Name}' -> '{Value}'", $"{AliasVerb}-Byname"))
        {
            return;
        }

        var writer = GetProfileWriter();

        // Resolve the function up front so a bad name is rejected before the session is changed.
        var function = SessionState.InvokeCommand.GetCommand(Value, CommandTypes.Function) as FunctionInfo;
        if (function is not null && (Value.Contains(' ', StringComparison.Ordinal) || Value.Contains('"', StringComparison.Ordinal)))
        {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException($"Function name '{Value}' contains spaces or quotes and cannot be written to the profile. Use a simple name."),
                "InvalidFunctionName", ErrorCategory.InvalidArgument, Value));
        }

        InvokeAliasCommand($"{AliasVerb}-Alias", new Dictionary<string, object?>
        {
            ["Name"] = Name,
            ["Value"] = Value,
            ["Description"] = Description,
            ["Option"] = Option,
            ["PassThru"] = PassThru.IsPresent,
            ["Scope"] = Scope,
            ["Force"] = Force.IsPresent,
        });

        if (replaceExisting)
        {
            var removed = writer.Remove(Name);
            WriteVerbose($"Removed {removed} existing entr{(removed == 1 ? "y" : "ies")} for '{Name}' from {writer.ProfilePath}.");
        }

        var line = ProfileAliasWriter.FormatAliasCommand(AliasVerb, Name, Value, Option, Scope, Force.IsPresent, Description);
        writer.Append(line, function?.Name, function?.Definition);
        WriteVerbose($"Persisted to {writer.ProfilePath}: {line}");
    }

    /// <summary>
    /// The alias command exactly as it is written to the profile.
    /// </summary>
    public override string ToString() =>
        ProfileAliasWriter.FormatAliasCommand(AliasVerb, Name, Value, Option, Scope, Force.IsPresent, Description);
}
