using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Lists TCP and UDP connections in any state, not just listeners.</para>
/// <para type="description">A netstat replacement that shows every socket - established, listening, closing - with
/// the owning process where the platform tool can report it. Use this to see who your machine is actually talking
/// to right now, which is the first thing to check when a connection misbehaves. See also Get-ListeningPort for a
/// narrower, listener only view.</para>
/// <example>
/// <para>Everything</para>
/// <code>Get-NetworkConnection</code>
/// </example>
/// <example>
/// <para>Only established connections</para>
/// <code>Get-NetworkConnection -State ESTABLISHED</code>
/// </example>
/// <example>
/// <para>What is process 4521 connected to?</para>
/// <code>Get-NetworkConnection | Where-Object ProcessId -eq 4521</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "NetworkConnection")]
[Alias("gnc", "connections")]
[OutputType(typeof(NetworkConnectionInfo))]
[ExperimentalCmdlet("It depends on netstat, lsof or ss to enumerate sockets and their owners. Details may be missing on some systems.")]
public sealed class GetNetworkConnectionCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Only show connections in this state, e.g. ESTABLISHED, LISTEN, TIME_WAIT. Wildcards are allowed. UDP sockets rarely report a state.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? State { get; set; }

    /// <summary>
    /// <para type="description">TCP, UDP or All. Defaults to All.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("TCP", "UDP", "All")]
    public string Protocol { get; set; } = "All";

    /// <summary>
    /// <para type="description">Only show connections to or from this remote address or hostname.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? RemoteAddress { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var connections = NetworkConnections.Discover();
        if (connections.Count == 0)
        {
            WriteWarning("No connections were returned. The platform tool (netstat, lsof or ss) may be missing or you may need elevated rights.");
            return;
        }

        IEnumerable<NetworkConnections.Connection> rows = connections;

        if (Protocol != "All")
        {
            rows = rows.Where(c => c.Protocol == Protocol);
        }

        if (!string.IsNullOrEmpty(State))
        {
            var pattern = new WildcardPattern(State, WildcardOptions.IgnoreCase);
            rows = rows.Where(c => c.State is not null && pattern.IsMatch(c.State));
        }

        if (!string.IsNullOrEmpty(RemoteAddress))
        {
            var pattern = new WildcardPattern($"*{RemoteAddress}*", WildcardOptions.IgnoreCase);
            rows = rows.Where(c => c.RemoteAddress is not null && pattern.IsMatch(c.RemoteAddress));
        }

        foreach (var row in rows.OrderBy(c => c.Protocol, StringComparer.Ordinal).ThenBy(c => c.LocalPort))
        {
            WriteObject(new NetworkConnectionInfo
            {
                Protocol = row.Protocol,
                LocalAddress = row.LocalAddress,
                LocalPort = row.LocalPort,
                RemoteAddress = row.RemoteAddress,
                RemotePort = row.RemotePort,
                State = row.State,
                ProcessId = row.ProcessId,
                ProcessName = row.ProcessName,
            });
        }
    }
}
