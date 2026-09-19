using System.Net;
using PowerPlug.Cmdlets.Networking;

namespace PowerPlug.Tests.Cmdlets;

public class FindNetworkDeviceCmdletTests
{
    [Theory]
    [InlineData("192.168.1.10/24", "192.168.1.0", 24)]
    [InlineData("10.0.0.5/22", "10.0.0.0", 22)]
    [InlineData("192.168.1.1/32", "192.168.1.1", 32)]
    public void ParseCidr_NormalisesToNetworkAddress(string cidr, string expectedNetwork, int expectedPrefix)
    {
        var (network, prefixLength) = FindNetworkDeviceCmdlet.ParseCidr(cidr);

        Assert.Equal(IPAddress.Parse(expectedNetwork), network);
        Assert.Equal(expectedPrefix, prefixLength);
    }

    [Theory]
    [InlineData("not-a-cidr")]
    [InlineData("192.168.1.1")]
    [InlineData("192.168.1.1/33")]
    [InlineData("::1/64")]
    public void ParseCidr_ReturnsNullNetworkForInvalidInput(string cidr)
    {
        var (network, _) = FindNetworkDeviceCmdlet.ParseCidr(cidr);
        Assert.Null(network);
    }

    [Fact]
    public void HostAddresses_ExcludesNetworkAndBroadcastForSlash24()
    {
        var hosts = FindNetworkDeviceCmdlet.HostAddresses(IPAddress.Parse("192.168.1.0"), 24).ToArray();

        Assert.Equal(254, hosts.Length);
        Assert.Equal(IPAddress.Parse("192.168.1.1"), hosts[0]);
        Assert.Equal(IPAddress.Parse("192.168.1.254"), hosts[^1]);
    }

    [Fact]
    public void HostAddresses_Slash31ReturnsSingleAddress()
    {
        var hosts = FindNetworkDeviceCmdlet.HostAddresses(IPAddress.Parse("192.168.1.0"), 31).ToArray();
        Assert.Single(hosts);
    }
}
