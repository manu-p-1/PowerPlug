using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Tests whether a TCP port is accepting connections.</para>
/// <para type="description">Opens a TCP connection to the host and port and reports whether it succeeded and how long
/// it took. Unlike Test-Connection this checks the actual service, not just ICMP reachability. Ports can be piped in
/// to check several at once.</para>
/// <example>
/// <para>Check a web server</para>
/// <code>Test-Port example.com 443</code>
/// </example>
/// <example>
/// <para>Check several ports with a short timeout</para>
/// <code>22, 80, 443, 8080 | Test-Port -HostName server01 -TimeoutMs 500</code>
/// </example>
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "Port")]
[Alias("tp")]
[OutputType(typeof(PortTestResult))]
public sealed class TestPortCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Host name or IP address.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [Alias("ComputerName", "Server", "Host")]
    [ValidateNotNullOrEmpty]
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">TCP port, 1-65535.</para>
    /// </summary>
    [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
    [ValidateRange(1, 65535)]
    public int[] Port { get; set; } = [];

    /// <summary>
    /// <para type="description">Connection timeout in milliseconds. Defaults to 2000.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(50, 120_000)]
    public int TimeoutMs { get; set; } = 2000;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var port in Port)
        {
            var probe = TcpProbe.Connect(HostName, port, TimeSpan.FromMilliseconds(TimeoutMs));
            WriteObject(new PortTestResult
            {
                Host = HostName,
                Port = port,
                Open = probe.Open,
                LatencyMs = probe.Open ? Math.Round(probe.LatencyMs, 2) : null,
                Error = probe.Error,
            });
        }
    }
}
