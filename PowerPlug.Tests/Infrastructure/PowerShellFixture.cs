using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerPlug.Tests.Infrastructure;

/// <summary>
/// Hosts a real PowerShell runspace with the PowerPlug module imported. Shared across a test class via
/// <see cref="IClassFixture{TFixture}"/> so the (slow) runspace start up happens once.
/// </summary>
public sealed class PowerShellFixture : IDisposable
{
    /// <summary>
    /// Path to the module manifest in the test output folder.
    /// </summary>
    public static string ManifestPath { get; } = Path.Combine(AppContext.BaseDirectory, "PowerPlug.psd1");

    public Runspace Runspace { get; }

    public PowerShellFixture()
    {
        var state = InitialSessionState.CreateDefault2();
        if (OperatingSystem.IsWindows())
        {
            state.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
        }

        state.ImportPSModule([ManifestPath]);

        Runspace = RunspaceFactory.CreateRunspace(state);
        Runspace.Open();

        // Fail loudly if the module did not load; every other test depends on it.
        var expected = typeof(Base.PowerPlugCmdlet).Assembly.GetName().Version!.ToString(3);
        var check = Invoke("(Get-Module PowerPlug).Version.ToString()");
        if (check.Errors.Count > 0 || check.Results.Count != 1 || check.Results[0].ToString() != expected)
        {
            throw new InvalidOperationException(
                $"PowerPlug {expected} did not load from {ManifestPath}: " + string.Join("; ", check.Errors.Select(e => e.ToString())));
        }
    }

    /// <summary>
    /// Removes globals a test class created (variables, functions and aliases starting with "pp") so the
    /// shared runspace starts each class clean.
    /// </summary>
    public void Reset()
    {
        Invoke("""
            Get-Variable -Scope Global -Name pp* -ErrorAction SilentlyContinue | Remove-Variable -Scope Global -Force
            Get-ChildItem function:global:Get-Pp* -ErrorAction SilentlyContinue | Remove-Item
            Get-Alias -Scope Global -Name pp_* -ErrorAction SilentlyContinue | ForEach-Object { Remove-Alias -Name $_.Name -Scope Global -Force }
            """);
    }

    /// <summary>
    /// Runs a script in the shared runspace and captures output plus streams. Variables are set in the global
    /// scope before the script runs, so a key of "path" is available as $path.
    /// </summary>
    public InvocationResult Invoke(string script, IDictionary<string, object?>? variables = null)
    {
        using var ps = PowerShell.Create();
        ps.Runspace = Runspace;

        if (variables is not null)
        {
            foreach (var (name, value) in variables)
            {
                Runspace.SessionStateProxy.SetVariable(name, value);
            }
        }

        ps.AddScript(script);
        Collection<PSObject> results;
        try
        {
            results = ps.Invoke();
        }
        catch (RuntimeException ex)
        {
            return new InvocationResult([], [ex.ErrorRecord], [], [], ex);
        }

        var warnings = ps.Streams.Warning.Select(w => w.Message).ToList();
        return new InvocationResult(
            results,
            ps.Streams.Error.ToList(),
            warnings.Where(w => !w.Contains(" is experimental. ", StringComparison.Ordinal)).ToList(),
            ps.Streams.Verbose.Select(v => v.Message).ToList(),
            null)
        {
            ExperimentalWarnings = warnings.Where(w => w.Contains(" is experimental. ", StringComparison.Ordinal)).ToList(),
        };
    }

    /// <summary>
    /// Convenience for a single variable.
    /// </summary>
    public InvocationResult Invoke(string script, string variableName, object? value) =>
        Invoke(script, new Dictionary<string, object?> { [variableName] = value });

    public void Dispose() => Runspace.Dispose();
}

/// <summary>
/// Output and streams from one script invocation. Experimental cmdlet warnings are filtered out of
/// <see cref="Warnings"/> so tests only see warnings the cmdlet chose to emit.
/// </summary>
public sealed record InvocationResult(
    Collection<PSObject> Results,
    IReadOnlyList<ErrorRecord> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Verbose,
    RuntimeException? Terminating)
{
    /// <summary>
    /// The experimental cmdlet notices, kept apart from <see cref="Warnings"/>.
    /// </summary>
    public IReadOnlyList<string> ExperimentalWarnings { get; init; } = [];

    /// <summary>
    /// Base objects of the results, cast to T.
    /// </summary>
    public IReadOnlyList<T> As<T>() => Results.Select(r => Assert.IsType<T>(r.BaseObject)).ToList();

    /// <summary>
    /// The single result's base object, cast to T.
    /// </summary>
    public T Only<T>() => Assert.IsType<T>(Assert.Single(Results).BaseObject);

    /// <summary>
    /// Asserts there were no errors of any kind.
    /// </summary>
    public InvocationResult ShouldSucceed()
    {
        Assert.Null(Terminating);
        Assert.True(Errors.Count == 0, "Unexpected errors: " + string.Join("; ", Errors.Select(e => e.ToString())));
        return this;
    }

    /// <summary>
    /// Every exception the invocation produced, whether it terminated the pipeline or was written to the error stream.
    /// </summary>
    public IEnumerable<Exception> AllExceptions =>
        (Terminating is null ? [] : new[] { (Exception)Terminating })
            .Concat(Errors.Select(e => e.Exception));

    /// <summary>
    /// Asserts that an exception of the given type was produced and returns it.
    /// </summary>
    public T ShouldFailWith<T>() where T : Exception
    {
        var match = AllExceptions.OfType<T>().FirstOrDefault()
            ?? AllExceptions.Select(e => e.InnerException).OfType<T>().FirstOrDefault();
        Assert.True(match is not null, $"Expected {typeof(T).Name} but got: {string.Join("; ", AllExceptions.Select(e => e.GetType().Name + ": " + e.Message))}");
        return match!;
    }

    /// <summary>
    /// Asserts that some error occurred and returns the first message.
    /// </summary>
    public string ShouldFail()
    {
        var first = AllExceptions.FirstOrDefault();
        Assert.True(first is not null, "Expected an error but the invocation succeeded.");
        return first!.Message;
    }
}
