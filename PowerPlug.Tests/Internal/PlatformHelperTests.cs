using PowerPlug.Internal;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Internal;

public class ListeningPortOwnersTests
{
    [Fact]
    public void ParseNetstat_ReadsListeningTcpAndUdp()
    {
        const string output = """
              Proto  Local Address          Foreign Address        State           PID
              TCP    0.0.0.0:135            0.0.0.0:0              LISTENING       1234
              TCP    127.0.0.1:5000         127.0.0.1:5001         ESTABLISHED     999
              TCP    [::]:445               [::]:0                 LISTENING       4
              UDP    0.0.0.0:5353           *:*                                    777
            """;

        var owners = ListeningPortOwners.ParseNetstat(output);

        Assert.Equal(1234, owners[("TCP", 135)].ProcessId);
        Assert.Equal(4, owners[("TCP", 445)].ProcessId);
        Assert.Equal(777, owners[("UDP", 5353)].ProcessId);
        Assert.False(owners.ContainsKey(("TCP", 5000)));
    }

    [Fact]
    public void ParseLsof_ReadsListenersAndSkipsConnections()
    {
        // lsof -F pcn output: p=pid, c=command, n=name. Command names may contain spaces.
        const string output = """
            p41234
            cnode
            n*:3000
            p500
            cGoogle Chrome Helper
            n*:7000
            n*:7001
            p6000
            cSlack
            n192.168.1.5:52000->1.2.3.4:443
            """;

        var owners = ListeningPortOwners.ParseLsof(output, "TCP");

        Assert.Equal(new ListeningPortOwners.Owner(41234, "node"), owners[("TCP", 3000)]);
        Assert.Equal(new ListeningPortOwners.Owner(500, "Google Chrome Helper"), owners[("TCP", 7000)]);
        Assert.Equal(new ListeningPortOwners.Owner(500, "Google Chrome Helper"), owners[("TCP", 7001)]);
        Assert.Equal(3, owners.Count);
    }

    [Fact]
    public void ParseLsof_Udp()
    {
        const string output = """
            p300
            cmDNSResponder
            n*:5353
            """;

        var owners = ListeningPortOwners.ParseLsof(output, "UDP");
        Assert.Equal(300, owners[("UDP", 5353)].ProcessId);
        Assert.Equal("mDNSResponder", owners[("UDP", 5353)].ProcessName);
    }

    [Fact]
    public void ParseSs_ReadsUsersColumn()
    {
        const string output = """
            tcp   LISTEN 0      128          0.0.0.0:22        0.0.0.0:*    users:(("sshd",pid=812,fd=3))
            tcp   LISTEN 0      4096            [::]:5432         [::]:*    users:(("postgres",pid=1200,fd=5))
            udp   UNCONN 0      0            0.0.0.0:68        0.0.0.0:*    users:(("dhclient",pid=640,fd=6))
            tcp   LISTEN 0      128          0.0.0.0:80        0.0.0.0:*
            """;

        var owners = ListeningPortOwners.ParseSs(output);

        Assert.Equal(new ListeningPortOwners.Owner(812, "sshd"), owners[("TCP", 22)]);
        Assert.Equal(new ListeningPortOwners.Owner(1200, "postgres"), owners[("TCP", 5432)]);
        Assert.Equal(new ListeningPortOwners.Owner(640, "dhclient"), owners[("UDP", 68)]);
        Assert.False(owners.ContainsKey(("TCP", 80)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage line\nanother")]
    public void Parsers_TolerateEmptyOrGarbage(string? output)
    {
        Assert.Empty(ListeningPortOwners.ParseNetstat(output));
        Assert.Empty(ListeningPortOwners.ParseLsof(output, "TCP"));
        Assert.Empty(ListeningPortOwners.ParseSs(output));
    }
}

public class PathEnvironmentTests
{
    [Fact]
    public void Split_DropsEmptyEntriesAndTrims()
    {
        var joined = string.Join(Path.PathSeparator, ["/a", "", " /b ", "/c"]);
        Assert.Equal(["/a", "/b", "/c"], PathEnvironment.Split(joined));
    }

    [Fact]
    public void Split_NullIsEmpty()
    {
        Assert.Empty(PathEnvironment.Split(null));
    }

    [Fact]
    public void Join_RoundTrips()
    {
        string[] entries = ["/a", "/b"];
        Assert.Equal(entries, PathEnvironment.Split(PathEnvironment.Join(entries)));
    }

    [Fact]
    public void Same_IgnoresTrailingSeparator()
    {
        using var temp = new TempDirectory();
        Assert.True(PathEnvironment.Same(temp.Path, temp.Path + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void Same_ResolvesRelativeSegmentsForRootedPaths()
    {
        using var temp = new TempDirectory();
        var child = temp.CreateDirectory("child");
        var viaParent = Path.Combine(child, "..", "child");
        Assert.True(PathEnvironment.Same(child, viaParent));
    }

    [Theory]
    [InlineData("User", EnvironmentVariableTarget.User)]
    [InlineData("Machine", EnvironmentVariableTarget.Machine)]
    [InlineData("Process", EnvironmentVariableTarget.Process)]
    [InlineData("anything", EnvironmentVariableTarget.Process)]
    public void ParseTarget(string text, EnvironmentVariableTarget expected)
    {
        Assert.Equal(expected, PathEnvironment.ParseTarget(text));
    }

    [Fact]
    public void TargetSupported_ProcessAlways()
    {
        Assert.True(PathEnvironment.TargetSupported(EnvironmentVariableTarget.Process));
        Assert.Equal(OperatingSystem.IsWindows(), PathEnvironment.TargetSupported(EnvironmentVariableTarget.User));
    }
}

public class DirectoryWalkerTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void EnumerateFiles_CountsSizesFilesAndDirectories()
    {
        _temp.WriteFile("a.txt", "12345");
        _temp.WriteFile(Path.Combine("sub", "b.txt"), "1234567890");
        _temp.WriteFile(Path.Combine("sub", "deeper", "c.txt"), "1");
        _temp.CreateDirectory("empty");

        var walker = new DirectoryWalker();
        var files = walker.EnumerateFiles(_temp.Path).Select(f => f.Name).OrderBy(n => n).ToArray();

        Assert.Equal(["a.txt", "b.txt", "c.txt"], files);
        Assert.Equal(16, walker.TotalBytes);
        Assert.Equal(3, walker.FileCount);
        Assert.Equal(3, walker.DirectoryCount);
        Assert.Equal(0, walker.SkippedCount);
    }

    [Fact]
    public void EnumerateFiles_NonRecursiveStopsAtTopLevel()
    {
        _temp.WriteFile("a.txt", "1");
        _temp.WriteFile(Path.Combine("sub", "b.txt"), "1");

        var walker = new DirectoryWalker();
        var files = walker.EnumerateFiles(_temp.Path, recurse: false).ToArray();

        Assert.Single(files);
        Assert.Equal(1, walker.DirectoryCount);
    }

    [Fact]
    public void EnumerateFiles_DoesNotFollowDirectorySymlinks()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Creating symlinks needs elevation on Windows.
        }

        _temp.WriteFile(Path.Combine("real", "big.txt"), new string('x', 1000));
        Directory.CreateSymbolicLink(_temp.Combine("link"), _temp.Combine("real"));

        var walker = new DirectoryWalker();
        _ = walker.EnumerateFiles(_temp.Path).ToArray();

        Assert.Equal(1000, walker.TotalBytes);
        Assert.Equal(1, walker.FileCount);
    }

    [Fact]
    public void DirectoriesDeepestFirst_OrdersChildrenBeforeParents()
    {
        var a = _temp.CreateDirectory("a");
        var ab = _temp.CreateDirectory(Path.Combine("a", "b"));
        var abc = _temp.CreateDirectory(Path.Combine("a", "b", "c"));

        var order = DirectoryWalker.DirectoriesDeepestFirst(_temp.Path);

        Assert.True(order.IndexOf(abc) < order.IndexOf(ab));
        Assert.True(order.IndexOf(ab) < order.IndexOf(a));
        Assert.Equal(_temp.Path, order[^1]);
    }
}
