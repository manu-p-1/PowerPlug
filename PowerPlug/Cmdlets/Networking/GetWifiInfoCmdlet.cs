using System.Management.Automation;
using System.Net.NetworkInformation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Reports the associated Wi-Fi network's signal strength, channel and security.</para>
/// <para type="description">Reads the platform's own wireless tooling (netsh on Windows, airport/system_profiler on
/// macOS, nmcli/iwconfig on Linux) since .NET has no cross platform Wi-Fi API. SignalToNoiseDb is the most reliable
/// indicator of interference or "full bars, no internet": below roughly 15 dB the link is unreliable even if the
/// signal percentage looks fine, because something nearby is raising the noise floor.</para>
/// <example>
/// <para>Current association</para>
/// <code>Get-WifiInfo</code>
/// </example>
/// <example>
/// <para>Watch the signal to noise ratio degrade over time</para>
/// <code>1..30 | ForEach-Object { Get-WifiInfo; Start-Sleep -Seconds 2 }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "WifiInfo")]
[Alias("wifi", "gwifi")]
[OutputType(typeof(WifiInfoResult))]
[ExperimentalCmdlet("It depends on netsh, airport/system_profiler or nmcli/iwconfig, which report different fields on each platform.")]
public sealed class GetWifiInfoCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The wireless interface name. Defaults to the first wireless interface that is up.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? Interface { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var interfaceName = Interface ?? FindWirelessInterface();
        if (interfaceName is null)
        {
            WriteError(new InvalidOperationException("No wireless interface was found or specified."), "NoWirelessInterface", ErrorCategory.ResourceUnavailable, null);
            return;
        }

        var reading = WifiInfo.Read(interfaceName);
        if (reading is null)
        {
            WriteError(new InvalidOperationException("The platform's wireless tool did not return any data. It may be missing, or the adapter may not be associated to a network."),
                "WifiUnavailable", ErrorCategory.ResourceUnavailable, interfaceName);
            return;
        }

        var value = reading.Value;
        int? snr = value.RssiDbm is not null && value.NoiseDbm is not null ? value.RssiDbm - value.NoiseDbm : null;

        WriteObject(new WifiInfoResult
        {
            Interface = interfaceName,
            Ssid = value.Ssid,
            Bssid = value.Bssid,
            SignalPercent = value.SignalPercent,
            RssiDbm = value.RssiDbm,
            NoiseDbm = value.NoiseDbm,
            SignalToNoiseDb = snr,
            Channel = value.Channel,
            Band = value.Channel is int channel ? BandOf(channel) : null,
            PhyMode = value.PhyMode,
            Security = value.Security,
            TransmitRateMbps = value.TransmitRateMbps,
        });
    }

    private static string? FindWirelessInterface() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .Select(n => n.Name)
            .FirstOrDefault();

    private static string BandOf(int channel) => channel switch
    {
        >= 1 and <= 14 => "2.4GHz",
        >= 32 and <= 177 => "5GHz",
        > 177 => "6GHz",
        _ => "Unknown",
    };
}
