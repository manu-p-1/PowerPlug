namespace PowerPlug.Attributes;

/// <summary>
/// Marks a cmdlet as experimental. Experimental cmdlets work, but they depend on platform behaviour,
/// external tooling, or make changes (like editing $PROFILE) that we are not yet ready to call stable.
/// <see cref="Base.PowerPlugCmdlet"/> emits a warning when the cmdlet starts.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class ExperimentalCmdletAttribute : Attribute
{
    /// <summary>
    /// The reason used when none is supplied.
    /// </summary>
    public const string DefaultReason =
        "Behaviour may change between releases and it may not work on every platform.";

    /// <summary>
    /// Why the cmdlet is experimental. Shown after the standard lead-in.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Marks the cmdlet as experimental with an optional reason.
    /// </summary>
    /// <param name="reason">A short explanation. Falls back to <see cref="DefaultReason"/>.</param>
    public ExperimentalCmdletAttribute(string? reason = null)
    {
        Reason = string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason;
    }

    /// <summary>
    /// Builds the full warning text for a cmdlet.
    /// </summary>
    public string FormatWarning(string cmdletName) => $"{cmdletName} is experimental. {Reason}";
}
