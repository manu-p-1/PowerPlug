using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using PowerPlug.Internal;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Internal;

public class TextEncodingsRejectionTests
{
    [Fact]
    public void Parse_UnknownNameThrows()
    {
        Assert.Throws<ArgumentException>(() => TextEncodings.Parse("EBCDIC"));
    }
}

public class JsonToPowerShellTests
{
    [Fact]
    public void ParseObject_MapsEveryJsonKind()
    {
        var result = JsonToPowerShell.ParseObject("""{"i":42,"big":9007199254740993,"f":1.5,"s":"x","t":true,"f2":false,"n":null,"a":[1,"two"],"o":{"k":"v"}}""");

        Assert.Equal(42L, result["i"]);
        Assert.Equal(9007199254740993L, result["big"]);
        Assert.Equal(1.5, result["f"]);
        Assert.Equal("x", result["s"]);
        Assert.Equal(true, result["t"]);
        Assert.Equal(false, result["f2"]);
        Assert.Null(result["n"]);
        Assert.Equal([1L, "two"], Assert.IsType<object?[]>(result["a"]));
        Assert.Equal("v", Assert.IsType<OrderedDictionary>(result["o"])["k"]);
    }

    [Fact]
    public void ParseObject_PreservesKeyOrder()
    {
        var result = JsonToPowerShell.ParseObject("""{"z":1,"a":2,"m":3}""");
        Assert.Equal(["z", "a", "m"], result.Keys.Cast<string>());
    }

    [Fact]
    public void ParseObject_RejectsNonObjectRoot()
    {
        Assert.Throws<InvalidOperationException>(() => JsonToPowerShell.ParseObject("[1,2]"));
    }

    [Fact]
    public void ParseObject_RejectsInvalidJson()
    {
        Assert.ThrowsAny<JsonException>(() => JsonToPowerShell.ParseObject("{not json"));
    }
}

[Collection(NetworkTestGroup.Name)]
public class TcpProbeTests
{
    [Fact]
    public void Connect_OpenPortSucceedsWithLatency()
    {
        using var listener = new LoopbackListener();
        var result = TcpProbe.Connect("127.0.0.1", listener.Port, TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(result.Open);
        Assert.Null(result.Error);
        Assert.True(result.LatencyMs >= 0);
    }

    [Fact]
    public void Connect_RefusedPortReportsSocketMessage()
    {
        var result = TcpProbe.Connect("127.0.0.1", LoopbackListener.FreePort(), TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.False(result.Open);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public void Connect_TimeoutIsReportedWithTheConfiguredDuration()
    {
        var result = TcpProbe.Connect("192.0.2.1", 80, TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
        Assert.False(result.Open);
        Assert.Equal("Connection timed out after 200 ms", result.Error);
    }

    [Fact]
    public void Connect_CallerCancellationIsReportedAsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = TcpProbe.Connect("192.0.2.1", 80, TimeSpan.FromSeconds(5), cts.Token);
        Assert.False(result.Open);
        Assert.Equal("Cancelled", result.Error);
    }
}

[Collection(NetworkTestGroup.Name)]
public class ProcessRunnerTests
{
    [Fact]
    public void TryRun_ReturnsStandardOutputOnSuccess()
    {
        var (file, args) = OperatingSystem.IsWindows() ? ("cmd.exe", "/c echo hello") : ("/bin/sh", "-c \"echo hello\"");
        var output = ProcessRunner.TryRun(file, args, TimeSpan.FromSeconds(10));
        Assert.Equal("hello", output?.Trim());
    }

    [Fact]
    public void TryRun_ReturnsNullOnNonZeroExit()
    {
        var (file, args) = OperatingSystem.IsWindows() ? ("cmd.exe", "/c exit 3") : ("/bin/sh", "-c \"exit 3\"");
        Assert.Null(ProcessRunner.TryRun(file, args, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TryRun_ReturnsNullWhenToolIsMissing()
    {
        Assert.Null(ProcessRunner.TryRun("powerplug-no-such-tool-" + Guid.NewGuid().ToString("N"), "", TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void TryRun_KillsAndReturnsNullOnTimeout()
    {
        var (file, args) = OperatingSystem.IsWindows() ? ("cmd.exe", "/c ping -n 30 127.0.0.1 > nul") : ("/bin/sh", "-c \"sleep 30\"");
        var started = DateTime.UtcNow;
        Assert.Null(ProcessRunner.TryRun(file, args, TimeSpan.FromMilliseconds(500)));
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10), "The process was not killed promptly.");
    }
}

public class PathEnvironmentNormalizeTests
{
    [Fact]
    public void Same_TreatsRootAsItself()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        Assert.True(PathEnvironment.Same(root, root));
    }

    [Fact]
    public void Same_DifferentDirectoriesAreNotSame()
    {
        using var temp = new TempDirectory();
        Assert.False(PathEnvironment.Same(temp.CreateDirectory("a"), temp.CreateDirectory("b")));
    }

    [Fact]
    public void Same_RelativeEntriesCompareTextually()
    {
        Assert.True(PathEnvironment.Same("bin", "bin"));
        Assert.False(PathEnvironment.Same("bin", "lib"));
    }

    [Fact]
    public void Same_CaseSensitivityFollowsThePlatform()
    {
        using var temp = new TempDirectory();
        var dir = temp.CreateDirectory("Mixed");
        var expected = OperatingSystem.IsLinux() ? false : true;
        Assert.Equal(expected, PathEnvironment.Same(dir, dir.ToUpperInvariant()));
    }
}

public class ModuleInfoTests
{
    [Fact]
    public void Version_IsTheInformationalVersionWithoutBuildMetadata()
    {
        var informational = typeof(ModuleInfo).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Single().InformationalVersion;

        Assert.DoesNotContain('+', ModuleInfo.Version);
        Assert.Equal(informational.Split('+')[0], ModuleInfo.Version);
        Assert.StartsWith(typeof(ModuleInfo).Assembly.GetName().Version!.ToString(3), ModuleInfo.Version, StringComparison.Ordinal);
    }

    [Fact]
    public void UserAgent_CarriesTheVersion()
    {
        Assert.Equal($"PowerPlug/{ModuleInfo.Version}", SharedHttp.UserAgent);
    }
}
