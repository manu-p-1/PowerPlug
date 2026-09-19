using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Lists listening TCP and UDP ports and the processes that own them.</para>
/// <para type="description">Lists sockets in the listening state on any platform. The socket list comes from .NET;
/// the owning process is looked up with netstat (Windows), lsof (macOS) or ss (Linux) and may be missing when the
/// tool is unavailable or the socket belongs to another user. Pipe to Stop-Process to free a port.</para>
/// <example>
/// <para>Everything listening</para>
/// <code>Get-ListeningPort</code>
/// </example>
/// <example>
/// <para>Who has port 3000, and stop it</para>
/// <code>Get-ListeningPort -Port 3000 | Stop-Process -Id { $_.ProcessId }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "ListeningPort")]
[Alias("lsport", "Get-OpenPort")]
[OutputType(typeof(ListeningPort))]
[ExperimentalCmdlet("It depends on netstat, lsof or ss to find owning processes. Process details may be missing on some systems.")]
public sealed class GetListeningPortCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Only show these ports.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateRange(1, 65535)]
    public int[]? Port { get; set; }

    /// <summary>
    /// <para type="description">TCP, UDP or All. Defaults to All.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("TCP", "UDP", "All")]
    public string Protocol { get; set; } = "All";

    /// <summary>
    /// <para type="description">Skip the process lookup for a faster result.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoProcess { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var sockets = new List<(string Protocol, IPEndPoint EndPoint)>();
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            if (Protocol is "TCP" or "All")
            {
                sockets.AddRange(properties.GetActiveTcpListeners().Select(e => ("TCP", e)));
            }

            if (Protocol is "UDP" or "All")
            {
                sockets.AddRange(properties.GetActiveUdpListeners().Select(e => ("UDP", e)));
            }
        }
        catch (NetworkInformationException ex)
        {
            ThrowTerminatingError(new ErrorRecord(ex, "NetworkInfoUnavailable", ErrorCategory.ResourceUnavailable, null));
        }

        if (Port is { Length: > 0 })
        {
            var wanted = Port.ToHashSet();
            sockets.RemoveAll(s => !wanted.Contains(s.EndPoint.Port));
        }

        var owners = NoProcess ? [] : ListeningPortOwners.Discover();
        if (!NoProcess && owners.Count == 0)
        {
            WriteWarning("Process lookup returned nothing. The platform tool may be missing or you may need elevated rights.");
        }

        var rows = sockets
            .Select(s =>
            {
                owners.TryGetValue((s.Protocol, s.EndPoint.Port), out var owner);
                return new ListeningPort
                {
                    Protocol = s.Protocol,
                    LocalAddress = s.EndPoint.Address.ToString(),
                    Port = s.EndPoint.Port,
                    ProcessId = owner.ProcessId == 0 ? null : owner.ProcessId,
                    ProcessName = owner.ProcessName,
                };
            })
            .OrderBy(r => r.Protocol, StringComparer.Ordinal)
            .ThenBy(r => r.Port)
            .ThenBy(r => r.LocalAddress, StringComparer.Ordinal);

        foreach (var row in rows)
        {
            WriteObject(row);
        }
    }
}
