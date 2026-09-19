using System.Diagnostics;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Queries DNS records directly, without relying on the OS resolver cache.</para>
/// <para type="description">Sends a raw DNS query over UDP to a chosen server (or the first server configured on an
/// active interface) and decodes the answer itself, so A, AAAA, CNAME, MX, TXT, NS, SOA and PTR records are all
/// available cross platform. This is useful for confirming what a domain's authoritative records actually say,
/// independent of whatever the system resolver has cached.</para>
/// <example>
/// <para>A records for a host</para>
/// <code>Get-DnsRecord example.com</code>
/// </example>
/// <example>
/// <para>Mail exchangers, queried against Cloudflare's resolver</para>
/// <code>Get-DnsRecord example.com -Type MX -Server 1.1.1.1</code>
/// </example>
/// <example>
/// <para>Reverse lookup</para>
/// <code>Get-DnsRecord 8.8.8.8 -Type PTR</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "DnsRecord")]
[Alias("dns", "resolve")]
[OutputType(typeof(DnsRecordResult))]
[ExperimentalCmdlet("It sends raw DNS queries over UDP and parses the response itself rather than using the OS resolver.")]
public sealed class GetDnsRecordCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The name to query. An IP address when -Type PTR is used.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The record type to request. Defaults to A.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateSet("A", "AAAA", "CNAME", "MX", "TXT", "NS", "SOA", "PTR", "SRV")]
    public string Type { get; set; } = "A";

    /// <summary>
    /// <para type="description">The DNS server to query. Defaults to the first server configured on an active network interface.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? Server { get; set; }

    /// <summary>
    /// <para type="description">The DNS server's port. Defaults to 53.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 65535)]
    public int Port { get; set; } = 53;

    /// <summary>
    /// <para type="description">Query timeout in milliseconds. Defaults to 3000.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(100, 60_000)]
    public int TimeoutMs { get; set; } = 3000;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var serverAddress = ResolveServer();
        if (serverAddress is null)
        {
            WriteError(new InvalidOperationException("No DNS server was specified and none could be discovered from the network configuration."),
                "NoDnsServer", ErrorCategory.ResourceUnavailable, Name);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        IReadOnlyList<DnsResolver.Record> records;
        try
        {
            records = DnsResolver.QueryAsync(Name, Type, serverAddress, Port, TimeSpan.FromMilliseconds(TimeoutMs), CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch (TimeoutException ex)
        {
            WriteError(ex, "DnsTimeout", ErrorCategory.OperationTimeout, Name);
            return;
        }
        catch (DnsResolutionException ex)
        {
            WriteError(ex, "DnsError", ErrorCategory.InvalidResult, Name);
            return;
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException)
        {
            WriteError(ex, "DnsQueryFailed", ErrorCategory.ConnectionError, Name);
            return;
        }

        stopwatch.Stop();

        if (records.Count == 0)
        {
            WriteWarning($"'{Name}' has no {Type} records.");
            return;
        }

        foreach (var record in records)
        {
            WriteObject(new DnsRecordResult
            {
                Name = string.IsNullOrEmpty(record.Name) ? Name : record.Name,
                Type = record.Type,
                Value = record.Value,
                TimeToLiveSeconds = record.TimeToLiveSeconds,
                Server = serverAddress.ToString(),
                QueryTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
            });
        }
    }

    private IPAddress? ResolveServer()
    {
        if (Server is not null)
        {
            return IPAddress.TryParse(Server, out var parsed) ? parsed : ResolveHostToAddress(Server);
        }

        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().DnsAddresses)
            .FirstOrDefault(a => a.AddressFamily is System.Net.Sockets.AddressFamily.InterNetwork or System.Net.Sockets.AddressFamily.InterNetworkV6);
    }

    private static IPAddress? ResolveHostToAddress(string host)
    {
        try
        {
            return Dns.GetHostAddresses(host).FirstOrDefault();
        }
        catch (System.Net.Sockets.SocketException)
        {
            return null;
        }
    }
}
