using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Lists the ARP / neighbor discovery cache: devices this machine has recently talked to on the local network.</para>
/// <para type="description">Reads the same cache that arp -a or ip neigh reports. This only shows hosts your machine
/// has already exchanged traffic with; use Find-NetworkDevice to actively sweep the subnet for hosts that have not
/// been seen yet.</para>
/// <example>
/// <para>Everything in the cache</para>
/// <code>Get-NetworkNeighbor</code>
/// </example>
/// <example>
/// <para>Look for an unexpected MAC vendor prefix</para>
/// <code>Get-NetworkNeighbor | Where-Object MacAddress -like 'de:ad:*'</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "NetworkNeighbor")]
[Alias("gnn", "arp")]
[OutputType(typeof(NetworkNeighborInfo))]
[ExperimentalCmdlet("It depends on arp or ip neigh to read the neighbor cache. Results may be sparse if the tool is unavailable.")]
public sealed class GetNetworkNeighborCmdlet : PowerPlugCmdlet
{
    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var neighbors = NetworkNeighbors.Discover();
        if (neighbors.Count == 0)
        {
            WriteWarning("The neighbor cache is empty or the platform tool (arp / ip neigh) is unavailable.");
            return;
        }

        foreach (var neighbor in neighbors.OrderBy(n => n.IPAddress, StringComparer.Ordinal))
        {
            WriteObject(new NetworkNeighborInfo
            {
                IPAddress = neighbor.IPAddress,
                MacAddress = neighbor.MacAddress,
                Interface = neighbor.Interface,
                State = neighbor.State,
            });
        }
    }
}
