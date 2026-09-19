using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Tests.Internal;

public class DnsResolverTests
{
    [Fact]
    public void BuildQuery_EncodesHeaderAndLabels()
    {
        var query = DnsResolver.BuildQuery(0x1234, "example.com", 1);

        Assert.Equal(0x12, query[0]);
        Assert.Equal(0x34, query[1]);
        Assert.Equal(1, BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(4, 2))); // QDCOUNT
        Assert.Equal(7, query[12]); // Length of "example" label
        Assert.Equal((byte)'e', query[13]);
        Assert.Equal(3, query[20]); // Length of "com" label
        Assert.Equal(0, query[24]); // Root label terminator
    }

    [Fact]
    public void ParseResponse_DecodesARecordWithNameCompression()
    {
        var response = BuildAResponse(0x1234, "example.com", "93.184.216.34", 300);

        var records = DnsResolver.ParseResponse(response, 0x1234);

        var record = Assert.Single(records);
        Assert.Equal("A", record.Type);
        Assert.Equal("93.184.216.34", record.Value);
        Assert.Equal(300u, record.TimeToLiveSeconds);
        Assert.Equal("example.com", record.Name);
    }

    [Fact]
    public void ParseResponse_ThrowsOnMismatchedTransactionId()
    {
        var response = BuildAResponse(0x1234, "example.com", "1.2.3.4", 60);

        Assert.Throws<DnsResolutionException>(() => DnsResolver.ParseResponse(response, 0x9999));
    }

    [Fact]
    public void ParseResponse_ThrowsOnNxDomain()
    {
        var response = BuildAResponse(0x1234, "example.com", "1.2.3.4", 60, responseCode: 3, answerCount: 0);

        Assert.Throws<DnsResolutionException>(() => DnsResolver.ParseResponse(response, 0x1234));
    }

    [Fact]
    public void ToReverseName_BuildsInAddrArpaForIPv4()
    {
        var name = DnsResolver.ToReverseName(IPAddress.Parse("8.8.4.4"));
        Assert.Equal("4.4.8.8.in-addr.arpa", name);
    }

    private static byte[] BuildAResponse(ushort transactionId, string name, string ip, uint ttl, int responseCode = 0, int answerCount = 1)
    {
        var buffer = new List<byte>();
        Append16(buffer, transactionId);
        Append16(buffer, (ushort)(0x8000 | responseCode)); // Response flag + response code.
        Append16(buffer, 1); // QDCOUNT
        Append16(buffer, (ushort)answerCount); // ANCOUNT
        Append16(buffer, 0); // NSCOUNT
        Append16(buffer, 0); // ARCOUNT

        foreach (var label in name.Split('.'))
        {
            buffer.Add((byte)label.Length);
            buffer.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
        }

        buffer.Add(0);
        Append16(buffer, 1); // QTYPE = A
        Append16(buffer, 1); // QCLASS = IN

        if (answerCount > 0)
        {
            buffer.Add(0xC0);
            buffer.Add(0x0C); // Pointer back to the question's name at offset 12.
            Append16(buffer, 1); // TYPE = A
            Append16(buffer, 1); // CLASS = IN
            Append32(buffer, ttl);
            Append16(buffer, 4); // RDLENGTH
            buffer.AddRange(IPAddress.Parse(ip).GetAddressBytes());
        }

        return [.. buffer];
    }

    private static void Append16(List<byte> buffer, int value)
    {
        buffer.Add((byte)(value >> 8));
        buffer.Add((byte)value);
    }

    private static void Append32(List<byte> buffer, uint value)
    {
        buffer.Add((byte)(value >> 24));
        buffer.Add((byte)(value >> 16));
        buffer.Add((byte)(value >> 8));
        buffer.Add((byte)value);
    }
}

public class NetworkConnectionsTests
{
    [Fact]
    public void ParseNetstat_KeepsAllStates()
    {
        const string output = """
              Proto  Local Address          Foreign Address        State           PID
              TCP    127.0.0.1:5000         127.0.0.1:5001         ESTABLISHED     999
              TCP    0.0.0.0:135            0.0.0.0:0              LISTENING       1234
              UDP    0.0.0.0:68             *:*                                    640
            """;

        var connections = NetworkConnections.ParseNetstat(output);

        Assert.Equal(3, connections.Count);
        var established = connections.Single(c => c.LocalPort == 5000);
        Assert.Equal("ESTABLISHED", established.State);
        Assert.Equal("127.0.0.1", established.RemoteAddress);
        Assert.Equal(5001, established.RemotePort);
    }

    [Fact]
    public void ParseLsof_ReadsEstablishedAndListeningWithState()
    {
        const string output = """
            COMMAND   PID   USER   FD   TYPE   DEVICE SIZE/OFF NODE NAME
            Chrome    500   user   50u  IPv4   0x1        0t0  TCP  192.168.1.5:54321->93.184.216.34:443 (ESTABLISHED)
            sshd      789   root   3u   IPv6   0x2        0t0  TCP  *:22 (LISTEN)
            """;

        var connections = NetworkConnections.ParseLsof(output);

        Assert.Equal(2, connections.Count);
        var established = connections.Single(c => c.ProcessId == 500);
        Assert.Equal("ESTABLISHED", established.State);
        Assert.Equal("93.184.216.34", established.RemoteAddress);
        Assert.Equal(443, established.RemotePort);

        var listening = connections.Single(c => c.ProcessId == 789);
        Assert.Equal("LISTEN", listening.State);
        Assert.Null(listening.RemoteAddress);
    }

    [Fact]
    public void ParseSs_ReadsStateAndPeer()
    {
        const string output = """
            tcp   ESTAB  0 0    192.168.1.5:54321   93.184.216.34:443   users:(("chrome",pid=999,fd=50))
            tcp   LISTEN 0 128  0.0.0.0:22          0.0.0.0:*           users:(("sshd",pid=812,fd=3))
            """;

        var connections = NetworkConnections.ParseSs(output);

        Assert.Equal(2, connections.Count);
        var established = connections.Single(c => c.State == "ESTAB");
        Assert.Equal(999, established.ProcessId);
        Assert.Equal("93.184.216.34", established.RemoteAddress);
        Assert.Equal(443, established.RemotePort);
    }
}

public class NetworkNeighborsTests
{
    [Fact]
    public void ParseWindowsArp_ReadsEntriesPerInterface()
    {
        const string output = """
            Interface: 192.168.1.10 --- 0x2
              Internet Address      Physical Address      Type
              192.168.1.1           aa-bb-cc-dd-ee-ff      dynamic
              192.168.1.20          11-22-33-44-55-66      static
            """;

        var neighbors = NetworkNeighbors.ParseWindowsArp(output);

        Assert.Equal(2, neighbors.Count);
        var gateway = neighbors.Single(n => n.IPAddress == "192.168.1.1");
        Assert.Equal("aa:bb:cc:dd:ee:ff", gateway.MacAddress);
        Assert.Equal("192.168.1.10", gateway.Interface);
    }

    [Fact]
    public void ParseMacArp_ReadsIncompleteAsNullMac()
    {
        const string output = """
            ? (192.168.1.1) at aa:bb:cc:dd:ee:ff on en0 ifscope [ethernet]
            ? (192.168.1.55) at (incomplete) on en0 ifscope [ethernet]
            """;

        var neighbors = NetworkNeighbors.ParseMacArp(output);

        Assert.Equal(2, neighbors.Count);
        Assert.Equal("aa:bb:cc:dd:ee:ff", neighbors[0].MacAddress);
        Assert.Null(neighbors[1].MacAddress);
    }

    [Fact]
    public void ParseIpNeigh_ReadsStateAndInterface()
    {
        const string output = """
            192.168.1.1 dev eth0 lladdr aa:bb:cc:dd:ee:ff REACHABLE
            192.168.1.99 dev eth0 FAILED
            """;

        var neighbors = NetworkNeighbors.ParseIpNeigh(output);

        Assert.Equal(2, neighbors.Count);
        var reachable = neighbors.Single(n => n.IPAddress == "192.168.1.1");
        Assert.Equal("aa:bb:cc:dd:ee:ff", reachable.MacAddress);
        Assert.Equal("REACHABLE", reachable.State);
    }
}

public class WifiInfoTests
{
    [Fact]
    public void ParseNetsh_ReadsSignalAndChannel()
    {
        const string output = """

                Name                   : Wi-Fi
                SSID                   : HomeNetwork
                BSSID                  : aa:bb:cc:dd:ee:ff
                Signal                 : 78%
                Radio type             : 802.11ac
                Channel                : 44
                Authentication         : WPA2-Personal
                Transmit rate (Mbps)   : 433
            """;

        var reading = WifiInfo.ParseNetsh(output);

        Assert.NotNull(reading);
        Assert.Equal("HomeNetwork", reading!.Value.Ssid);
        Assert.Equal(78, reading.Value.SignalPercent);
        Assert.Equal(44, reading.Value.Channel);
        Assert.Equal(433, reading.Value.TransmitRateMbps);
    }

    [Fact]
    public void ParseAirport_ComputesSignalToNoise()
    {
        const string output = """
                     SSID: HomeNetwork
                    BSSID: aa:bb:cc:dd:ee:ff
                agrCtlRSSI: -55
               agrCtlNoise: -92
                  channel: 44
                lastTxRate: 866
            """;

        var reading = WifiInfo.ParseAirport(output);

        Assert.NotNull(reading);
        Assert.Equal(-55, reading!.Value.RssiDbm);
        Assert.Equal(-92, reading.Value.NoiseDbm);
        Assert.Equal(44, reading.Value.Channel);
    }

    [Fact]
    public void ParseNmcli_KeepsOnlyActiveRow()
    {
        const string output = """
            no:OtherNetwork:11\:22\:33\:44\:55\:66:6:2437 MHz:130 Mbit/s:40:WPA2
            yes:HomeNetwork:aa\:bb\:cc\:dd\:ee\:ff:44:5220 MHz:867 Mbit/s:82:WPA2
            """;

        var reading = WifiInfo.ParseNmcli(output);

        Assert.NotNull(reading);
        Assert.Equal("HomeNetwork", reading!.Value.Ssid);
        Assert.Equal("aa:bb:cc:dd:ee:ff", reading.Value.Bssid);
        Assert.Equal(44, reading.Value.Channel);
        Assert.Equal(82, reading.Value.SignalPercent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parsers_TolerateMissingOutput(string? output)
    {
        Assert.Null(WifiInfo.ParseNetsh(output));
        Assert.Null(WifiInfo.ParseAirport(output));
        Assert.Null(WifiInfo.ParseNmcli(output));
        Assert.Null(WifiInfo.ParseIwconfig(output));
    }
}

public class VpnDetectorTests
{
    [Theory]
    [InlineData("utun3", "")]
    [InlineData("wg0", "")]
    [InlineData("eth0", "Tailscale Tunnel")]
    [InlineData("Ethernet 2", "TAP-Windows Adapter V9")]
    public void IsLikelyVpn_MatchesCommonTunnelNames(string name, string description)
    {
        Assert.True(VpnDetector.IsLikelyVpn(FakeNic(name, description, NetworkInterfaceType.Ethernet)));
    }

    [Fact]
    public void IsLikelyVpn_FalseForOrdinaryEthernet()
    {
        Assert.False(VpnDetector.IsLikelyVpn(FakeNic("Ethernet", "Intel(R) Ethernet Connection", NetworkInterfaceType.Ethernet)));
    }

    [Fact]
    public void IsLikelyVpn_TrueForPppType()
    {
        Assert.True(VpnDetector.IsLikelyVpn(FakeNic("ppp0", "", NetworkInterfaceType.Ppp)));
    }

    private static StubNetworkInterface FakeNic(string name, string description, NetworkInterfaceType type) =>
        new StubNetworkInterface(name, description, type);

    // Minimal stand-in since NetworkInterface has no public constructor; only the members VpnDetector reads are implemented.
    private sealed class StubNetworkInterface(string name, string description, NetworkInterfaceType type) : NetworkInterface
    {
        public override string Name => name;
        public override string Description => description;
        public override NetworkInterfaceType NetworkInterfaceType => type;
        public override string Id => name;
        public override OperationalStatus OperationalStatus => OperationalStatus.Up;
        public override long Speed => 0;
        public override bool SupportsMulticast => false;
        public override System.Net.NetworkInformation.PhysicalAddress GetPhysicalAddress() => System.Net.NetworkInformation.PhysicalAddress.None;
        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent) => false;
        public override IPInterfaceProperties GetIPProperties() => throw new NotSupportedException();
        public override IPInterfaceStatistics GetIPStatistics() => throw new NotSupportedException();
        public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new NotSupportedException();
    }
}


