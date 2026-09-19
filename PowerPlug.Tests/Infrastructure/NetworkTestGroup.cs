using System.Net;
using System.Net.Sockets;

namespace PowerPlug.Tests.Infrastructure;

/// <summary>
/// Test classes that bind loopback ports or spawn processes join this collection so they run one at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NetworkTestGroup
{
    public const string Name = "Network";
}

/// <summary>
/// A TCP listener on a free loopback port for the port cmdlets to hit.
/// </summary>
public sealed class LoopbackListener : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    public LoopbackListener()
    {
        _listener.Start();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// A port that nothing is listening on. There is a small window where something else could grab it, which is
    /// why port-using test classes share <see cref="NetworkTestGroup"/>.
    /// </summary>
    public static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    public void Dispose() => _listener.Stop();
}
