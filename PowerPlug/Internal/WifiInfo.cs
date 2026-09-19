using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Reads the associated Wi-Fi network's signal strength, channel and security from the platform's own wireless
/// tooling (there is no cross platform .NET API for this). Signal-to-noise ratio, when both figures are available,
/// is the most reliable indicator of interference: a healthy link is usually 20 dB or higher.
/// </summary>
internal static partial class WifiInfo
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(10);
    private const string AirportPath = "/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport";

    /// <summary>Raw fields read from the platform tool. Percentages and dBm figures may each be null depending on the platform.</summary>
    public readonly record struct Reading(
        string? Ssid,
        string? Bssid,
        int? SignalPercent,
        int? RssiDbm,
        int? NoiseDbm,
        int? Channel,
        string? PhyMode,
        string? Security,
        double? TransmitRateMbps);

    /// <summary>
    /// Reads the current Wi-Fi association details for the given interface name (ignored on platforms whose tool
    /// only supports the primary adapter). Returns null when no platform tool could answer.
    /// </summary>
    public static Reading? Read(string interfaceName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ParseNetsh(ProcessRunner.TryRun("netsh", "wlan show interfaces", ToolTimeout));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (File.Exists(AirportPath))
            {
                var reading = ParseAirport(ProcessRunner.TryRun(AirportPath, "-I", ToolTimeout));
                if (reading is not null)
                {
                    return reading;
                }
            }

            return ParseSystemProfiler(ProcessRunner.TryRun("system_profiler", "SPAirPortDataType", ToolTimeout));
        }

        return ParseNmcli(ProcessRunner.TryRun("nmcli", $"-t -f active,ssid,bssid,chan,freq,rate,signal,security dev wifi", ToolTimeout))
            ?? ParseIwconfig(ProcessRunner.TryRun("iwconfig", interfaceName, ToolTimeout));
    }

    /// <summary>Parses `netsh wlan show interfaces`. Exposed for testing.</summary>
    internal static Reading? ParseNetsh(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        var ssid = FieldOf(output, @"^\s*SSID\s*:\s*(?<v>.+)$");
        var bssid = FieldOf(output, @"^\s*BSSID\s*:\s*(?<v>.+)$");
        var signal = FieldOf(output, @"^\s*Signal\s*:\s*(?<v>\d+)%");
        var channel = FieldOf(output, @"^\s*Channel\s*:\s*(?<v>\d+)");
        var radio = FieldOf(output, @"^\s*Radio type\s*:\s*(?<v>.+)$");
        var auth = FieldOf(output, @"^\s*Authentication\s*:\s*(?<v>.+)$");
        var rate = FieldOf(output, @"^\s*Transmit rate \(Mbps\)\s*:\s*(?<v>[\d.]+)");

        var signalPercent = signal is not null ? int.Parse(signal, CultureInfo.InvariantCulture) : (int?)null;

        return new Reading(
            Ssid: ssid,
            Bssid: bssid,
            SignalPercent: signalPercent,
            RssiDbm: signalPercent is null ? null : PercentToDbm(signalPercent.Value),
            NoiseDbm: null,
            Channel: channel is not null ? int.Parse(channel, CultureInfo.InvariantCulture) : null,
            PhyMode: radio,
            Security: auth,
            TransmitRateMbps: rate is not null ? double.Parse(rate, CultureInfo.InvariantCulture) : null);
    }

    /// <summary>Parses `airport -I`. Exposed for testing.</summary>
    internal static Reading? ParseAirport(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        var ssid = FieldOf(output, @"^\s*SSID\s*:\s*(?<v>.+)$");
        var bssid = FieldOf(output, @"^\s*BSSID\s*:\s*(?<v>.+)$");
        var rssi = FieldOf(output, @"^\s*agrCtlRSSI\s*:\s*(?<v>-?\d+)");
        var noise = FieldOf(output, @"^\s*agrCtlNoise\s*:\s*(?<v>-?\d+)");
        var channel = FieldOf(output, @"^\s*channel\s*:\s*(?<v>\d+)");
        var rate = FieldOf(output, @"^\s*lastTxRate\s*:\s*(?<v>[\d.]+)");
        var auth = FieldOf(output, @"^\s*link auth\s*:\s*(?<v>.+)$");

        var rssiDbm = rssi is not null ? int.Parse(rssi, CultureInfo.InvariantCulture) : (int?)null;
        var noiseDbm = noise is not null ? int.Parse(noise, CultureInfo.InvariantCulture) : (int?)null;

        return new Reading(
            Ssid: ssid,
            Bssid: bssid,
            SignalPercent: rssiDbm is null ? null : DbmToPercent(rssiDbm.Value),
            RssiDbm: rssiDbm,
            NoiseDbm: noiseDbm,
            Channel: channel is not null ? int.Parse(channel, CultureInfo.InvariantCulture) : null,
            PhyMode: null,
            Security: auth,
            TransmitRateMbps: rate is not null ? double.Parse(rate, CultureInfo.InvariantCulture) : null);
    }

    /// <summary>Parses `system_profiler SPAirPortDataType` as a fallback when airport is unavailable. Exposed for testing.</summary>
    internal static Reading? ParseSystemProfiler(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        var ssid = FieldOf(output, @"^\s*Current Network Information:\s*$\s*^\s*(?<v>.+):\s*$", RegexOptions.Multiline);
        var channel = FieldOf(output, @"^\s*Channel\s*:\s*(?<v>\d+)");
        var phy = FieldOf(output, @"^\s*PHY Mode\s*:\s*(?<v>.+)$");
        var security = FieldOf(output, @"^\s*Security\s*:\s*(?<v>.+)$");
        var signal = FieldOf(output, @"^\s*Signal\s*/\s*Noise\s*:\s*(?<v>-?\d+)\s*dBm\s*/\s*-?\d+\s*dBm");
        var noise = FieldOf(output, @"^\s*Signal\s*/\s*Noise\s*:\s*-?\d+\s*dBm\s*/\s*(?<v>-?\d+)\s*dBm");

        if (ssid is null && channel is null)
        {
            return null;
        }

        var rssiDbm = signal is not null ? int.Parse(signal, CultureInfo.InvariantCulture) : (int?)null;
        var noiseDbm = noise is not null ? int.Parse(noise, CultureInfo.InvariantCulture) : (int?)null;

        return new Reading(
            Ssid: ssid,
            Bssid: null,
            SignalPercent: rssiDbm is null ? null : DbmToPercent(rssiDbm.Value),
            RssiDbm: rssiDbm,
            NoiseDbm: noiseDbm,
            Channel: channel is not null ? int.Parse(channel, CultureInfo.InvariantCulture) : null,
            PhyMode: phy,
            Security: security,
            TransmitRateMbps: null);
    }

    /// <summary>Parses `nmcli -t -f active,ssid,bssid,chan,freq,rate,signal,security dev wifi`, keeping the active row. Exposed for testing.</summary>
    internal static Reading? ParseNmcli(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = SplitNmcli(line);
            if (fields.Length < 8 || !fields[0].Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var signalPercent = int.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : (int?)null;

            return new Reading(
                Ssid: fields[1],
                Bssid: fields[2],
                SignalPercent: signalPercent,
                RssiDbm: signalPercent is null ? null : PercentToDbm(signalPercent.Value),
                NoiseDbm: null,
                Channel: int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) ? c : null,
                PhyMode: null,
                Security: fields[7],
                TransmitRateMbps: ParseNmcliRate(fields[5]));
        }

        return null;
    }

    private static double? ParseNmcliRate(string field)
    {
        var digits = new string(field.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        return double.TryParse(digits, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    // nmcli -t escapes colons in field values with a backslash, since ':' is also the field separator.
    private static string[] SplitNmcli(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\' && i + 1 < line.Length)
            {
                current.Append(line[i + 1]);
                i++;
                continue;
            }

            if (line[i] == ':')
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(line[i]);
        }

        fields.Add(current.ToString());
        return [.. fields];
    }

    /// <summary>Parses `iwconfig &lt;interface&gt;` as a fallback when NetworkManager/nmcli is unavailable. Exposed for testing.</summary>
    internal static Reading? ParseIwconfig(string? output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        var ssid = FieldOf(output, "ESSID:\"(?<v>[^\"]*)\"");
        var rssi = FieldOf(output, @"Signal level=(?<v>-?\d+) dBm");
        var noise = FieldOf(output, @"Noise level=(?<v>-?\d+) dBm");
        var rate = FieldOf(output, @"Bit Rate=(?<v>[\d.]+) Mb/s");
        var freq = FieldOf(output, @"Frequency:(?<v>[\d.]+) GHz");

        if (ssid is null && rssi is null)
        {
            return null;
        }

        var rssiDbm = rssi is not null ? int.Parse(rssi, CultureInfo.InvariantCulture) : (int?)null;
        var noiseDbm = noise is not null ? int.Parse(noise, CultureInfo.InvariantCulture) : (int?)null;

        return new Reading(
            Ssid: ssid,
            Bssid: null,
            SignalPercent: rssiDbm is null ? null : DbmToPercent(rssiDbm.Value),
            RssiDbm: rssiDbm,
            NoiseDbm: noiseDbm,
            Channel: freq is not null ? FrequencyToChannel(double.Parse(freq, CultureInfo.InvariantCulture)) : null,
            PhyMode: null,
            Security: null,
            TransmitRateMbps: rate is not null ? double.Parse(rate, CultureInfo.InvariantCulture) : null);
    }

    private static int FrequencyToChannel(double ghz)
    {
        var mhz = ghz * 1000;
        if (mhz is >= 2412 and <= 2484)
        {
            return mhz == 2484 ? 14 : (int)Math.Round((mhz - 2412) / 5) + 1;
        }

        return (int)Math.Round((mhz - 5000) / 5);
    }

    private static string? FieldOf(string text, string pattern, RegexOptions options = RegexOptions.Multiline)
    {
        var match = Regex.Match(text, pattern, options, TimeSpan.FromSeconds(2));
        return match.Success ? match.Groups["v"].Value.Trim() : null;
    }

    // Rough RSSI<->quality conversion matching the curve Windows and most drivers use, for platforms that only report one of the two.
    private static int DbmToPercent(int dbm) => dbm switch
    {
        >= -50 => 100,
        <= -100 => 0,
        _ => 2 * (dbm + 100),
    };

    private static int PercentToDbm(int percent) => -100 + (percent / 2);
}
