using System.Management.Automation;
using PowerPlug.Cmdlets.Diagnostics;
using PowerPlug.Cmdlets.Shell;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class MeasureScriptBlockCmdletTests
{
    [Fact]
    public void Summarize_ComputesStatistics()
    {
        var result = MeasureScriptBlockCmdlet.Summarize([5, 1, 3, 2, 4], failures: 1);

        Assert.Equal(5, result.Iterations);
        Assert.Equal(1, result.Failures);
        Assert.Equal(15, result.TotalMs);
        Assert.Equal(3, result.AverageMs);
        Assert.Equal(3, result.MedianMs);
        Assert.Equal(1, result.MinMs);
        Assert.Equal(5, result.MaxMs);
        Assert.Equal(Math.Round(Math.Sqrt(2), 4), result.StdDevMs);
    }

    [Fact]
    public void Summarize_SingleSample()
    {
        var result = MeasureScriptBlockCmdlet.Summarize([7.5], 0);
        Assert.Equal(7.5, result.MedianMs);
        Assert.Equal(0, result.StdDevMs);
    }
}

public class InvokeRetryCmdletTests
{
    [Fact]
    public void NextDelay_WithoutJitterIsCappedByMax()
    {
        var cmdlet = new InvokeRetryCmdlet { MaxDelayMilliseconds = 100 };
        Assert.Equal(100, cmdlet.NextDelay(500));
        Assert.Equal(50, cmdlet.NextDelay(50));
    }

    [Fact]
    public void NextDelay_WithJitterStaysWithin25Percent()
    {
        var cmdlet = new InvokeRetryCmdlet { Jitter = true, MaxDelayMilliseconds = 10_000 };
        for (var i = 0; i < 200; i++)
        {
            Assert.InRange(cmdlet.NextDelay(1000), 750, 1250);
        }
    }

    [Fact]
    public void NextDelay_ZeroStaysZero()
    {
        var cmdlet = new InvokeRetryCmdlet { Jitter = true };
        Assert.Equal(0, cmdlet.NextDelay(0));
    }
}

public class ImportDotEnvCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _prefix = "PPTEST_" + Guid.NewGuid().ToString("N")[..8] + "_";

    public void Dispose()
    {
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key.ToString()!.StartsWith(_prefix, StringComparison.Ordinal))
            {
                Environment.SetEnvironmentVariable(entry.Key.ToString()!, null);
            }
        }

        _temp.Dispose();
    }

    [Fact]
    public void SetsVariablesAndReturnsEntriesWithPassThru()
    {
        var file = _temp.WriteFile(".env", $"{_prefix}A=1\n{_prefix}B=\"two words\"\n");
        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Equal("1", Environment.GetEnvironmentVariable(_prefix + "A"));
        Assert.Equal("two words", Environment.GetEnvironmentVariable(_prefix + "B"));
        var entries = h.OutputOf<DotEnvEntry>();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.False(e.Overwritten));
    }

    [Fact]
    public void ExistingVariablesAreKeptUnlessForce()
    {
        Environment.SetEnvironmentVariable(_prefix + "KEEP", "original");
        var file = _temp.WriteFile(".env", $"{_prefix}KEEP=changed\n");

        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Equal("original", Environment.GetEnvironmentVariable(_prefix + "KEEP"));

        var forced = new CmdletHarness<ImportDotEnvCmdlet>();
        forced.Cmdlet.Path = [file];
        forced.Cmdlet.Force = true;
        forced.Cmdlet.PassThru = true;
        forced.Run();
        Assert.Equal("changed", Environment.GetEnvironmentVariable(_prefix + "KEEP"));
        Assert.True(forced.Only<DotEnvEntry>().Overwritten);
    }

    [Fact]
    public void WhatIfSetsNothing()
    {
        var file = _temp.WriteFile(".env", $"{_prefix}X=1\n");
        var h = new CmdletHarness<ImportDotEnvCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Empty(h.Errors);
        Assert.Equal([_prefix + "X"], h.ShouldProcessTargets);
        Assert.Null(Environment.GetEnvironmentVariable(_prefix + "X"));
    }

    [Fact]
    public void MultipleFilesAreLoadedInOrderAndLaterFilesWinWithForce()
    {
        var first = _temp.WriteFile("a.env", $"{_prefix}M=first\n{_prefix}ONLY_A=a\n");
        var second = _temp.WriteFile("b.env", $"{_prefix}M=second\n");

        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [first, second];
        h.Cmdlet.Force = true;
        h.Run();

        Assert.Equal("second", Environment.GetEnvironmentVariable(_prefix + "M"));
        Assert.Equal("a", Environment.GetEnvironmentVariable(_prefix + "ONLY_A"));
        Assert.Equal(2, h.Verbose.Count(v => v.StartsWith("Loaded ", StringComparison.Ordinal)));
    }

    [Fact]
    public void ExpandResolvesReferences()
    {
        var file = _temp.WriteFile(".env", $"{_prefix}BASE=/opt\n{_prefix}BIN=${{{_prefix}BASE}}/bin\n");
        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.Expand = true;
        h.Run();
        Assert.Equal("/opt/bin", Environment.GetEnvironmentVariable(_prefix + "BIN"));
    }

    [Fact]
    public void MissingFileWritesError()
    {
        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("missing.env")];
        h.Run();
        Assert.Equal(ErrorCategory.ObjectNotFound, Assert.Single(h.Errors).CategoryInfo.Category);
    }

    [Fact]
    public void MalformedFileWritesError()
    {
        var file = _temp.WriteFile(".env", "this is not valid\n");
        var h = new CmdletHarness<ImportDotEnvCmdlet>();
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Equal(ErrorCategory.InvalidData, Assert.Single(h.Errors).CategoryInfo.Category);
    }
}

// PATH is process wide and Get-ListeningPort spawns tools found through PATH, so these share the sequential collection.
[Collection(NetworkTestGroup.Name)]
public class EnvironmentPathCmdletTests : IDisposable
{
    private readonly string? _originalPath = Environment.GetEnvironmentVariable("PATH");
    private readonly TempDirectory _temp = new();

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PATH", _originalPath);
        _temp.Dispose();
    }

    private static void SetPath(params string[] entries) =>
        Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, entries));

    [Fact]
    public void Get_FlagsMissingAndDuplicateEntries()
    {
        var existing = _temp.CreateDirectory("exists");
        var missing = _temp.Combine("missing");
        SetPath(existing, missing, existing);

        var h = new CmdletHarness<GetEnvironmentPathCmdlet>();
        h.Run();

        var rows = h.OutputOf<PathEntry>();
        Assert.Equal(3, rows.Count);
        Assert.True(rows[0].Exists);
        Assert.False(rows[0].Duplicate);
        Assert.False(rows[1].Exists);
        Assert.True(rows[2].Duplicate);
        Assert.Equal([0, 1, 2], rows.Select(r => r.Index));
    }

    [Fact]
    public void Get_EmptyPathWarns()
    {
        SetPath();
        var h = new CmdletHarness<GetEnvironmentPathCmdlet>();
        h.Run();
        Assert.Empty(h.Output);
        Assert.Single(h.Warnings);
    }

    [Fact]
    public void Add_AppendsNewDirectoryAndSkipsExisting()
    {
        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [b, a];
        h.Cmdlet.PassThru = true;
        h.Run();

        var rows = h.OutputOf<PathEntry>();
        Assert.Equal([a, b], rows.Select(r => r.Path));
        Assert.Contains(h.Verbose, v => v.Contains("Already in PATH", StringComparison.Ordinal));
        Assert.Equal(string.Join(Path.PathSeparator, a, b), Environment.GetEnvironmentVariable("PATH"));
    }

    [Fact]
    public void Add_PrependPutsEntryFirst()
    {
        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [b];
        h.Cmdlet.Prepend = true;
        h.Run();

        Assert.StartsWith(b, Environment.GetEnvironmentVariable("PATH"), StringComparison.Ordinal);
    }

    [Fact]
    public void Add_MissingDirectoryWritesErrorWithoutForce()
    {
        var a = _temp.CreateDirectory("a");
        var missing = _temp.Combine("missing");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [missing];
        h.Run();

        Assert.Equal(ErrorCategory.ObjectNotFound, h.OnlyError("DirectoryNotFound").CategoryInfo.Category);
        Assert.Equal(a, Environment.GetEnvironmentVariable("PATH"));
    }

    [Fact]
    public void Add_ForceAllowsMissingDirectory()
    {
        var a = _temp.CreateDirectory("a");
        var missing = _temp.Combine("missing");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [missing];
        h.Cmdlet.Force = true;
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Empty(h.Errors);
        Assert.EndsWith(missing, Environment.GetEnvironmentVariable("PATH"), StringComparison.Ordinal);
        Assert.False(h.OutputOf<PathEntry>().Single(r => r.Path == missing).Exists);
    }

    [Fact]
    public void Add_RelativePathIsResolvedToFullPath()
    {
        var a = _temp.CreateDirectory("a");
        SetPath(a);
        var original = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_temp.Path);
        try
        {
            var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
            h.Cmdlet.Path = ["b"];
            h.Cmdlet.Force = true;
            h.Run();
            Assert.EndsWith(_temp.Combine("b"), Environment.GetEnvironmentVariable("PATH"), StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    [Fact]
    public void Add_WhatIfChangesNothing()
    {
        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [b];
        h.Run();
        Assert.Equal(a, Environment.GetEnvironmentVariable("PATH"));
    }

    [Fact]
    public void Remove_ByPath()
    {
        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a, b);

        var h = new CmdletHarness<RemoveEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [a + Path.DirectorySeparatorChar];
        h.Run();
        Assert.Equal(b, Environment.GetEnvironmentVariable("PATH"));
    }

    [Fact]
    public void Remove_EntryNotInPathIsANoOp()
    {
        var a = _temp.CreateDirectory("a");
        SetPath(a);

        var h = new CmdletHarness<RemoveEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("not-there")];
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Empty(h.Errors);
        Assert.Equal(a, Environment.GetEnvironmentVariable("PATH"));
        Assert.Equal(a, h.Only<PathEntry>().Path);
    }

    [Fact]
    public void Remove_WhatIfChangesNothing()
    {
        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a, b);

        var h = new CmdletHarness<RemoveEnvironmentPathCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [a];
        h.Run();

        Assert.Equal(string.Join(Path.PathSeparator, a, b), Environment.GetEnvironmentVariable("PATH"));
        Assert.Single(h.ShouldProcessTargets);
    }

    [Fact]
    public void Remove_MissingAndDuplicates()
    {
        var a = _temp.CreateDirectory("a");
        var missing = _temp.Combine("missing");
        SetPath(a, missing, a, missing);

        var h = new CmdletHarness<RemoveEnvironmentPathCmdlet>();
        h.Cmdlet.RemoveMissing = true;
        h.Cmdlet.RemoveDuplicates = true;
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Equal(a, Environment.GetEnvironmentVariable("PATH"));
        Assert.Single(h.OutputOf<PathEntry>());
    }

    [Fact]
    public void Remove_WithNoCriteriaTerminates()
    {
        var h = new CmdletHarness<RemoveEnvironmentPathCmdlet>();
        var ex = Assert.Throws<TerminatingErrorException>(() => h.Run());
        Assert.StartsWith("NothingToRemove", ex.Record.FullyQualifiedErrorId, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Machine")]
    public void NonProcessTargetsFallBackToProcessOffWindows(string target)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "User and Machine targets are real on Windows.");

        SetPath(_temp.Path);
        var h = new CmdletHarness<GetEnvironmentPathCmdlet>();
        h.Cmdlet.Target = target;
        h.Run();
        Assert.Single(h.Warnings);
        Assert.Equal("Process", h.Only<PathEntry>().Target);
    }

    [Fact]
    public void Add_UserTargetFallsBackToProcessOffWindows()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "User target is real on Windows.");

        var a = _temp.CreateDirectory("a");
        var b = _temp.CreateDirectory("b");
        SetPath(a);

        var h = new CmdletHarness<AddEnvironmentPathCmdlet>();
        h.Cmdlet.Path = [b];
        h.Cmdlet.Target = "User";
        h.Run();

        Assert.Single(h.Warnings);
        Assert.EndsWith(b, Environment.GetEnvironmentVariable("PATH"), StringComparison.Ordinal);
    }
}

public class GetSystemInfoCmdletTests
{
    [Fact]
    public void FillsCoreFieldsWithoutASession()
    {
        var h = new CmdletHarness<GetSystemInfoCmdlet>();
        h.Run();
        var info = h.Only<SystemInfo>();
        Assert.Equal(Environment.MachineName, info.ComputerName);
        Assert.Equal(Environment.UserName, info.UserName);
        Assert.False(string.IsNullOrEmpty(info.OS));
        Assert.Equal(Environment.ProcessorCount, info.ProcessorCount);
        Assert.True(info.Uptime > TimeSpan.Zero);
        Assert.Contains(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(), info.Architecture, StringComparison.Ordinal);
        Assert.Equal(PowerPlug.Internal.ModuleInfo.Version, info.PowerPlugVersion);
        Assert.Equal("unknown", info.PowerShellVersion);
        Assert.Equal(Environment.IsPrivilegedProcess, info.IsElevated);
        Assert.Equal(TimeZoneInfo.Local.Id, info.TimeZone);
        if (info.TotalMemoryBytes is not null)
        {
            Assert.True(info.TotalMemoryBytes > 0);
            Assert.False(string.IsNullOrEmpty(info.TotalMemory));
        }
    }
}
