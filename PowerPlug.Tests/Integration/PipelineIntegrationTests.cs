using System.Collections.Specialized;
using System.Management.Automation;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Integration;

/// <summary>
/// Runs cmdlets through the real parameter binder and pipeline to catch attribute and binding mistakes the
/// mocked harness cannot see.
/// </summary>
[Collection(PowerShellTestGroup.Name)]
public class PipelineIntegrationTests(PowerShellFixture ps)
{
    [Fact]
    public void Base64_RoundTripsThroughPipeline()
    {
        var result = ps.Invoke("'round trip' | ConvertTo-Base64 | ConvertFrom-Base64").ShouldSucceed();
        Assert.Equal("round trip", result.Only<string>());
    }

    [Fact]
    public void Base64_ParameterSetsAreDistinct()
    {
        ps.Invoke("ConvertTo-Base64 -InputString 'x' -Path 'y'").ShouldFailWith<ParameterBindingException>();
    }

    [Fact]
    public void Aliases_ResolveToCmdlets()
    {
        var result = ps.Invoke("'hi' | tobase64 | frombase64").ShouldSucceed();
        Assert.Equal("hi", result.Only<string>());
    }

    [Fact]
    public void TestPort_AcceptsPortsFromPipeline()
    {
        var result = ps.Invoke("1, 2 | Test-Port -HostName 127.0.0.1 -TimeoutMs 200").ShouldSucceed();
        var rows = result.As<PortTestResult>();
        Assert.Equal([1, 2], rows.Select(r => r.Port));
    }

    [Fact]
    public void ValidateRange_RejectsOutOfRangePort()
    {
        ps.Invoke("Test-Port 127.0.0.1 70000").ShouldFailWith<ParameterBindingException>();
    }

    [Fact]
    public void ValidateSet_RejectsUnknownAlgorithm()
    {
        ps.Invoke("Get-StringHash 'x' -Algorithm CRC32").ShouldFailWith<ParameterBindingException>();
    }

    [Fact]
    public void ValidateSet_AlgorithmIsCaseInsensitive()
    {
        var result = ps.Invoke("(Get-StringHash 'x' -Algorithm sha1).Algorithm").ShouldSucceed();
        Assert.Equal("SHA1", result.Only<string>());
    }

    [Fact]
    public void ConvertToHashTable_FromJson()
    {
        var result = ps.Invoke("'{\"a\":1,\"b\":{\"c\":[1,2,{\"d\":true}]}}' | ConvertFrom-Json | ConvertTo-HashTable -Recurse").ShouldSucceed();
        var ht = result.Only<OrderedDictionary>();
        Assert.Equal(1L, ht["a"]);
        var b = Assert.IsType<OrderedDictionary>(ht["b"]);
        var c = Assert.IsType<object?[]>(b["c"]);
        Assert.Equal(true, Assert.IsType<OrderedDictionary>(c[2])["d"]);
    }

    [Fact]
    public void ConvertToHashTable_ResultCanBeSplatted()
    {
        var result = ps.Invoke("""
            function Test-Splat { param($Name, $Count) "$Name=$Count" }
            $p = [pscustomobject]@{ Name = 'n'; Count = 3 } | ConvertTo-HashTable
            Test-Splat @p
            """).ShouldSucceed();
        Assert.Equal("n=3", result.Only<string>());
    }

    [Fact]
    public void InvokeRetry_ReturnsOutputAfterTransientFailures()
    {
        var result = ps.Invoke("""
            $script:attempts = 0
            Invoke-Retry { $script:attempts++; if ($script:attempts -lt 3) { throw "flaky" }; "ok after $script:attempts" } -MaxAttempts 5 -DelayMilliseconds 10
            """).ShouldSucceed();

        Assert.Equal("ok after 3", result.Only<string>());
        Assert.Equal(2, result.Warnings.Count);
    }

    [Fact]
    public void InvokeRetry_ThrowsLastErrorWhenExhausted()
    {
        var message = ps.Invoke("Invoke-Retry { throw 'always' } -MaxAttempts 2 -DelayMilliseconds 1 -ErrorAction Stop").ShouldFail();
        Assert.Contains("always", message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvokeRetry_PassesArgumentList()
    {
        var result = ps.Invoke("Invoke-Retry { param($a, $b) $a + $b } -ArgumentList 2, 3").ShouldSucceed();
        Assert.Equal(5, result.Only<int>());
    }

    [Fact]
    public void InvokeRetry_ExponentialBackoffGrowsTheWaitAndIsCapped()
    {
        var result = ps.Invoke("""
            $script:n = 0
            Invoke-Retry { $script:n++; if ($script:n -lt 4) { throw 'x' }; 'done' } -MaxAttempts 4 -DelayMilliseconds 10 -ExponentialBackoff -MaxDelayMilliseconds 25 -Verbose
            """).ShouldSucceed();

        Assert.Equal("done", result.Only<string>());
        var waits = result.Verbose.Where(v => v.StartsWith("Waiting ", StringComparison.Ordinal)).ToList();
        Assert.Equal(["Waiting 10 ms before attempt 2.", "Waiting 20 ms before attempt 3.", "Waiting 25 ms before attempt 4."], waits);
        Assert.Contains(result.Verbose, v => v == "Succeeded on attempt 4.");
    }

    [Fact]
    public void InvokeRetry_SingleAttemptDoesNotWait()
    {
        var result = ps.Invoke("Invoke-Retry { throw 'once' } -MaxAttempts 1 -DelayMilliseconds 5000 -Verbose -ErrorAction Stop");
        result.ShouldFail();
        Assert.DoesNotContain(result.Verbose, v => v.StartsWith("Waiting", StringComparison.Ordinal));
    }

    [Fact]
    public void InvokeRetry_NonTerminatingErrorsDoNotTriggerARetry()
    {
        var result = ps.Invoke("""
            $script:n = 0
            Invoke-Retry { $script:n++; Write-Error 'soft'; 'value' } -MaxAttempts 3 -DelayMilliseconds 1
            $script:n
            """);

        Assert.Equal(["value", "1"], result.Results.Select(r => r.ToString()));
        Assert.Single(result.Errors);
    }

    [Fact]
    public void MeasureScriptBlock_ReportsIterationsAndFailures()
    {
        var result = ps.Invoke("Measure-ScriptBlock { Start-Sleep -Milliseconds 1 } -Iterations 5 -WarmUp 1").ShouldSucceed();
        var bench = result.Only<BenchmarkResult>();
        Assert.Equal(5, bench.Iterations);
        Assert.Equal(0, bench.Failures);
        Assert.True(bench.MinMs >= 0);
        Assert.True(bench.MaxMs >= bench.MedianMs);
        Assert.True(bench.MedianMs >= bench.MinMs);
        Assert.Equal(Math.Round(bench.TotalMs, 2), Math.Round(bench.AverageMs * bench.Iterations, 2), 1);
    }

    [Fact]
    public void MeasureScriptBlock_CountsThrowingIterations()
    {
        var result = ps.Invoke("Measure-ScriptBlock { throw 'x' } -Iterations 5").ShouldSucceed();
        Assert.Equal(5, result.Only<BenchmarkResult>().Failures);
        // Three individual warnings, then one summary once failures pass three.
        Assert.Equal(4, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.StartsWith("5 of 5 iterations threw", StringComparison.Ordinal));
    }

    [Fact]
    public void MeasureScriptBlock_PassesArgumentListAndIgnoresWarmUpFailures()
    {
        var result = ps.Invoke("""
            $script:seen = 0
            Measure-ScriptBlock { param($x) if ($script:seen++ -eq 0) { throw 'warm' }; $x * 2 } -Iterations 3 -WarmUp 1 -ArgumentList 21
            """).ShouldSucceed();

        var bench = result.Only<BenchmarkResult>();
        Assert.Equal(3, bench.Iterations);
        Assert.Equal(0, bench.Failures);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void WatchCommand_StopsAtCountAndPassesThrough()
    {
        var result = ps.Invoke("Watch-Command { 'tick' } -IntervalSeconds 0.1 -Count 3 -NoClear -PassThru 6>$null").ShouldSucceed();
        Assert.Equal(["tick", "tick", "tick"], result.Results.Select(r => r.ToString()));
    }

    [Fact]
    public void WatchCommand_StopsWhenUntilIsTrue()
    {
        var result = ps.Invoke("""
            $script:n = 0
            Watch-Command { $script:n++; $script:n } -IntervalSeconds 0.1 -Until { $_[0] -ge 3 } -NoClear -PassThru 6>$null
            """).ShouldSucceed();
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public void WatchCommand_WritesHeaderAndOutputToTheInformationStream()
    {
        var result = ps.Invoke("Watch-Command { 'hello there' } -Interval 0.1 -Count 1 -NoClear 6>&1").ShouldSucceed();
        var text = string.Concat(result.Results.Select(r => r.ToString()));
        Assert.Contains("Every 0.1s: 'hello there'", text, StringComparison.Ordinal);
        Assert.Contains("(run 1)", text, StringComparison.Ordinal);
        Assert.Contains("hello there", text[text.IndexOf("(run 1)", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void WatchCommand_ScriptErrorsAndUntilErrorsAreReportedNotThrown()
    {
        var result = ps.Invoke("Watch-Command { throw 'boom' } -IntervalSeconds 0.1 -Count 2 -NoClear -PassThru -Until { throw 'until broke' } 6>$null").ShouldSucceed();
        Assert.Equal(["ERROR: boom", "ERROR: boom"], result.Results.Select(r => r.ToString()));
        Assert.Contains(result.Warnings, w => w.Contains("until broke", StringComparison.Ordinal));
    }

    [Fact]
    public void GetSystemInfo_ReadsPowerShellVersionFromSession()
    {
        var result = ps.Invoke("(Get-SystemInfo).PowerShellVersion").ShouldSucceed();
        var version = result.Only<string>();
        Assert.StartsWith("7.", version, StringComparison.Ordinal);
    }

    [Fact]
    public void RelativePaths_ResolveAgainstPowerShellLocation()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("rel.txt", "abc");
        var result = ps.Invoke("""
            Push-Location $ppDir
            try { (Compare-Hash -Path ./rel.txt -Signature ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad).Match }
            finally { Pop-Location }
            """, "ppDir", temp.Path).ShouldSucceed();
        Assert.True(result.Only<bool>());
    }

    [Fact]
    public void Wildcards_ExpandThroughTheProvider()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("a.log", "1");
        temp.WriteFile("b.log", "2");
        temp.WriteFile("c.txt", "3");

        var result = ps.Invoke("Rename-BatchItem (Join-Path $ppDir '*.log') -Pattern 'log' -Replacement 'old' -WhatIf", "ppDir", temp.Path).ShouldSucceed();

        var previews = result.As<RenameResult>();
        Assert.Equal(["a.log", "b.log"], previews.Select(r => r.OldName).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(previews, r => Assert.Equal("Preview", r.Reason));
        Assert.True(File.Exists(temp.Combine("a.log")));
    }

    [Fact]
    public void Wildcards_WithNoMatchWriteAPathNotFoundError()
    {
        using var temp = new TempDirectory();
        var result = ps.Invoke("Move-Trash (Join-Path $ppDir '*.nothing') -WhatIf", "ppDir", temp.Path);
        var error = Assert.Single(result.Errors);
        Assert.StartsWith("PathNotFound", error.FullyQualifiedErrorId, StringComparison.Ordinal);
    }

    [Fact]
    public void GetChildItem_PipesIntoFileCmdlets()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("one.txt", "abc");
        temp.WriteFile("two.txt", "abc");

        var result = ps.Invoke("Get-ChildItem $ppDir -File | Compare-Hash -Signature ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "ppDir", temp.Path).ShouldSucceed();

        var rows = result.As<HashComparisonResult>();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.Match));
    }

    [Fact]
    public void ImportDotEnv_VariablesVisibleThroughEnvDrive()
    {
        using var temp = new TempDirectory();
        var name = "PP_INT_" + Guid.NewGuid().ToString("N")[..6];
        var file = temp.WriteFile(".env", $"{name}=from-dotenv\n");
        try
        {
            var result = ps.Invoke($"Import-DotEnv $ppFile; $env:{name}", "ppFile", file).ShouldSucceed();
            Assert.Equal("from-dotenv", result.Only<string>());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void RenameBatchItem_FromGetChildItem()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("IMG_001.jpg", "x");
        temp.WriteFile("IMG_002.jpg", "x");

        var result = ps.Invoke("Get-ChildItem $ppDir | Rename-BatchItem -Pattern '^IMG_' -Replacement 'holiday-'", "ppDir", temp.Path).ShouldSucceed();

        Assert.Equal(2, result.As<RenameResult>().Count(r => r.Renamed));
        Assert.True(File.Exists(temp.Combine("holiday-001.jpg")));
    }
}
