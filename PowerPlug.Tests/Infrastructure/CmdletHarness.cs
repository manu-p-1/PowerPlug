using System.Management.Automation;
using System.Reflection;

namespace PowerPlug.Tests.Infrastructure;

/// <summary>
/// Runs a cmdlet outside of PowerShell by swapping in a mocked <see cref="ICommandRuntime"/>.
/// This is the fast path for cmdlets that do not touch SessionState. Output, errors, warnings, verbose and
/// information messages are captured for assertions, and ShouldProcess / ShouldContinue can be scripted.
/// Wildcard paths are not supported here because there is no provider; use the PowerShell fixture for those.
/// </summary>
/// <typeparam name="TCmdlet">The cmdlet under test.</typeparam>
public sealed class CmdletHarness<TCmdlet> : IDisposable where TCmdlet : PSCmdlet, new()
{
    private static readonly MethodInfo BeginProcessing = Method("BeginProcessing");
    private static readonly MethodInfo ProcessRecord = Method("ProcessRecord");
    private static readonly MethodInfo EndProcessing = Method("EndProcessing");
    private static readonly MethodInfo SetParameterSetName = typeof(PSCmdlet)
        .GetMethod("SetParameterSetName", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(nameof(PSCmdlet), "SetParameterSetName");

    /// <summary>The cmdlet instance. Set parameters on it before calling <see cref="Run"/>.</summary>
    public TCmdlet Cmdlet { get; } = new();

    /// <summary>The mocked runtime, available for extra expectations.</summary>
    public ICommandRuntime2 Runtime { get; } = Substitute.For<ICommandRuntime2>();

    /// <summary>Objects passed to WriteObject, in order. Enumerated collections are flattened like PowerShell does.</summary>
    public List<object?> Output { get; } = [];

    /// <summary>Error records passed to WriteError.</summary>
    public List<ErrorRecord> Errors { get; } = [];

    /// <summary>Warnings passed to WriteWarning, not counting the experimental cmdlet notice.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>True when the cmdlet emitted the experimental notice.</summary>
    public bool WarnedExperimental { get; private set; }

    /// <summary>Verbose messages passed to WriteVerbose.</summary>
    public List<string> Verbose { get; } = [];

    /// <summary>Progress records passed to WriteProgress.</summary>
    public List<ProgressRecord> Progress { get; } = [];

    /// <summary>Information records passed to WriteInformation (this is where Write-Host style output lands).</summary>
    public List<InformationRecord> Information { get; } = [];

    /// <summary>Targets passed to ShouldProcess, useful for asserting -WhatIf style behaviour.</summary>
    public List<string> ShouldProcessTargets { get; } = [];

    /// <summary>Queries passed to ShouldContinue.</summary>
    public List<string> ShouldContinueQueries { get; } = [];

    public CmdletHarness(bool shouldProcess = true, bool shouldContinue = true)
    {
        Runtime.WriteObject(Arg.Do<object?>(Output.Add));
        Runtime.WriteObject(Arg.Do<object?>(Output.Add), false);
        Runtime.WriteObject(Arg.Do<object?>(AddEnumerated), true);
        Runtime.WriteError(Arg.Do<ErrorRecord>(Errors.Add));
        Runtime.WriteWarning(Arg.Do<string>(w =>
        {
            if (w.Contains(" is experimental. ", StringComparison.Ordinal))
            {
                WarnedExperimental = true;
            }
            else
            {
                Warnings.Add(w);
            }
        }));
        Runtime.WriteVerbose(Arg.Do<string>(Verbose.Add));
        Runtime.WriteProgress(Arg.Do<ProgressRecord>(Progress.Add));
        Runtime.WriteProgress(Arg.Any<long>(), Arg.Do<ProgressRecord>(Progress.Add));
        Runtime.WriteInformation(Arg.Do<InformationRecord>(Information.Add));

        Runtime.ShouldProcess(Arg.Do<string>(ShouldProcessTargets.Add)).Returns(shouldProcess);
        Runtime.ShouldProcess(Arg.Do<string>(ShouldProcessTargets.Add), Arg.Any<string>()).Returns(shouldProcess);
        Runtime.ShouldProcess(Arg.Do<string>(ShouldProcessTargets.Add), Arg.Any<string>(), Arg.Any<string>()).Returns(shouldProcess);

        Runtime.ShouldContinue(Arg.Do<string>(ShouldContinueQueries.Add), Arg.Any<string>()).Returns(shouldContinue);
        Runtime.ShouldContinue(Arg.Do<string>(ShouldContinueQueries.Add), Arg.Any<string>(), ref Arg.Any<bool>(), ref Arg.Any<bool>()).Returns(shouldContinue);

        // No host: cmdlets must behave as they would under a non-interactive host.
        Runtime.Host.Returns((System.Management.Automation.Host.PSHost?)null);

        Runtime.ThrowTerminatingError(Arg.Do<ErrorRecord>(record => throw new TerminatingErrorException(record)));

        Cmdlet.CommandRuntime = Runtime;
    }

    /// <summary>
    /// Sets the active parameter set name, which the binder would normally do.
    /// </summary>
    public CmdletHarness<TCmdlet> WithParameterSet(string name)
    {
        SetParameterSetName.Invoke(Cmdlet, [name]);
        return this;
    }

    /// <summary>
    /// Runs Begin, Process (once) and End, then disposes the cmdlet if it is disposable.
    /// </summary>
    public CmdletHarness<TCmdlet> Run()
    {
        try
        {
            Invoke(BeginProcessing);
            Invoke(ProcessRecord);
            Invoke(EndProcessing);
        }
        finally
        {
            Dispose();
        }

        return this;
    }

    /// <summary>
    /// Runs Begin, then Process once per record (each action sets the pipeline bound parameters for that
    /// record), then End. Use this for cmdlets that accumulate across records.
    /// </summary>
    public CmdletHarness<TCmdlet> Run(params Action<TCmdlet>[] records)
    {
        try
        {
            Invoke(BeginProcessing);
            foreach (var record in records)
            {
                record(Cmdlet);
                Invoke(ProcessRecord);
            }

            Invoke(EndProcessing);
        }
        finally
        {
            Dispose();
        }

        return this;
    }

    /// <summary>
    /// Error ids (the part before the comma in FullyQualifiedErrorId) of every error written.
    /// </summary>
    public IEnumerable<string> ErrorIds => Errors.Select(e => e.FullyQualifiedErrorId.Split(',')[0]);

    /// <summary>
    /// Asserts exactly one error was written and that it carries the given id.
    /// </summary>
    public ErrorRecord OnlyError(string errorId)
    {
        var error = Assert.Single(Errors);
        Assert.Equal(errorId, error.FullyQualifiedErrorId.Split(',')[0]);
        return error;
    }

    public void Dispose() => (Cmdlet as IDisposable)?.Dispose();

    /// <summary>
    /// Typed view over <see cref="Output"/>.
    /// </summary>
    public IReadOnlyList<T> OutputOf<T>() => Output.OfType<T>().ToList();

    /// <summary>
    /// The single output item, cast to T.
    /// </summary>
    public T Only<T>() => Assert.IsType<T>(Assert.Single(Output));

    private void Invoke(MethodInfo method)
    {
        try
        {
            method.Invoke(Cmdlet, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private void AddEnumerated(object? value)
    {
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                Output.Add(item);
            }
        }
        else
        {
            Output.Add(value);
        }
    }

    private static MethodInfo Method(string name) =>
        typeof(Cmdlet).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(nameof(Cmdlet), name);
}

/// <summary>
/// Raised by the harness when a cmdlet calls ThrowTerminatingError, so tests can assert on it.
/// </summary>
public sealed class TerminatingErrorException(ErrorRecord record) : Exception(record.Exception?.Message ?? record.ErrorDetails?.Message)
{
    public ErrorRecord Record { get; } = record;
}
