using System.Management.Automation;
using System.Reflection;
using PowerPlug.Attributes;

namespace PowerPlug.Base;

/// <summary>
/// Base class for every PowerPlug cmdlet. It handles the experimental warning and offers a few helpers
/// that most cmdlets need, such as path resolution and consistent error records.
/// </summary>
public abstract class PowerPlugCmdlet : PSCmdlet
{
    /// <summary>
    /// Emits the experimental warning when the concrete cmdlet carries <see cref="ExperimentalCmdletAttribute"/>.
    /// </summary>
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        var experimental = GetType().GetCustomAttribute<ExperimentalCmdletAttribute>();
        if (experimental is not null)
        {
            var cmdlet = GetType().GetCustomAttribute<CmdletAttribute>();
            var name = cmdlet is null ? GetType().Name : $"{cmdlet.VerbName}-{cmdlet.NounName}";
            WriteWarning(experimental.FormatWarning(name));
        }
    }

    /// <summary>
    /// Resolves a PowerShell path (relative, rooted, or PSDrive based) to a file system path.
    /// The path does not need to exist. Outside of a runspace (unit tests) it falls back to plain
    /// file system resolution against the process working directory.
    /// </summary>
    /// <param name="path">The path as typed by the user.</param>
    /// <returns>The fully qualified file system path.</returns>
    protected string ResolvePath(string path)
    {
        if (CommandRuntime is not null && SessionState is not null)
        {
            return GetUnresolvedProviderPathFromPSPath(path);
        }

        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Resolves a path that may contain wildcards to every matching file system path. A path that names an
    /// existing item is returned as itself even if it contains bracket characters, which is what piped FullName
    /// values need. Paths without wildcards are returned resolved but not required to exist. Wildcards that match
    /// nothing produce a non-terminating error.
    /// </summary>
    protected IEnumerable<string> ResolvePaths(string path)
    {
        var literal = ResolvePath(path);
        if (!WildcardPattern.ContainsWildcardCharacters(path) || Path.Exists(literal))
        {
            yield return literal;
            yield break;
        }

        IEnumerable<string> resolved;
        try
        {
            resolved = SessionState is null
                ? ExpandWithoutProvider(path)
                : SessionState.Path.GetResolvedProviderPathFromPSPath(path, out _);
        }
        catch (SessionStateException ex)
        {
            WriteError(ex, "PathNotFound", ErrorCategory.ObjectNotFound, path);
            yield break;
        }

        var any = false;
        foreach (var item in resolved)
        {
            any = true;
            yield return item;
        }

        if (!any)
        {
            WriteError(new ItemNotFoundException($"No items match '{path}'."), "PathNotFound", ErrorCategory.ObjectNotFound, path);
        }
    }

    // Used only under the test harness, where there is no provider. Supports a wildcard in the last segment.
    private static IEnumerable<string> ExpandWithoutProvider(string path)
    {
        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full);
        var pattern = Path.GetFileName(full);
        return directory is not null && Directory.Exists(directory)
            ? Directory.EnumerateFileSystemEntries(directory, pattern)
            : [];
    }

    /// <summary>
    /// Writes a non-terminating error built from an exception.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="errorId">A short, stable identifier for the error.</param>
    /// <param name="category">The error category.</param>
    /// <param name="target">The object the cmdlet was working on, if any.</param>
    protected void WriteError(Exception exception, string errorId, ErrorCategory category, object? target = null) =>
        WriteError(new ErrorRecord(exception, errorId, category, target));

    /// <summary>
    /// Writes a non-terminating error for a common file system failure, mapping the exception type to
    /// a sensible category.
    /// </summary>
    /// <param name="exception">The exception raised by the file system operation.</param>
    /// <param name="path">The path that was being processed.</param>
    /// <param name="writing">True when the operation was a write, false for a read.</param>
    protected void WriteFileError(Exception exception, string path, bool writing = false)
    {
        var (id, category) = exception switch
        {
            FileNotFoundException or DirectoryNotFoundException => ("PathNotFound", ErrorCategory.ObjectNotFound),
            UnauthorizedAccessException => ("AccessDenied", ErrorCategory.PermissionDenied),
            _ => ("IOError", writing ? ErrorCategory.WriteError : ErrorCategory.ReadError),
        };

        WriteError(exception, id, category, path);
    }
}
