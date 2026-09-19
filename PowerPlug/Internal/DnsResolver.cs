using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PowerPlug.Internal;

/// <summary>
/// A minimal DNS client implemented directly against RFC 1035 wire format over UDP. Used because
/// <see cref="System.Net.Dns"/> only ever returns A/AAAA style host entries and has no way to ask for MX, TXT, NS,
/// SOA or PTR records, and there is no first party alternative on non-Windows platforms.
/// </summary>
internal static class DnsResolver
{
    /// <summary>A decoded resource record.</summary>
    public readonly record struct Record(string Name, string Type, string Value, uint TimeToLiveSeconds);

    private static readonly Dictionary<string, ushort> TypeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = 1,
        ["NS"] = 2,
        ["CNAME"] = 5,
        ["SOA"] = 6,
        ["PTR"] = 12,
        ["MX"] = 15,
        ["TXT"] = 16,
        ["AAAA"] = 28,
        ["SRV"] = 33,
    };

    /// <summary>
    /// Sends a single query over UDP and returns the answer records. Throws <see cref="TimeoutException"/> when the
    /// server does not respond in time, and <see cref="DnsResolutionException"/> when the server returns an error code.
    /// </summary>
    public static async Task<IReadOnlyList<Record>> QueryAsync(string name, string recordType, IPAddress server, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!TypeCodes.TryGetValue(recordType, out var typeCode))
        {
            throw new ArgumentException($"Unsupported record type '{recordType}'.", nameof(recordType));
        }

        var queryName = recordType.Equals("PTR", StringComparison.OrdinalIgnoreCase) && IPAddress.TryParse(name, out var ip)
            ? ToReverseName(ip)
            : name;

        var transactionId = (ushort)Random.Shared.Next(ushort.MinValue, ushort.MaxValue + 1);
        var query = BuildQuery(transactionId, queryName, typeCode);

        using var udp = new UdpClient(server.AddressFamily);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        await udp.SendAsync(query, new IPEndPoint(server, port), cts.Token).ConfigureAwait(false);

        UdpReceiveResult response;
        try
        {
            response = await udp.ReceiveAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No response from DNS server {server} within {timeout.TotalMilliseconds:0} ms.");
        }

        return ParseResponse(response.Buffer, transactionId);
    }

    /// <summary>
    /// Builds a query packet for a single question. Exposed for testing.
    /// </summary>
    internal static byte[] BuildQuery(ushort transactionId, string name, ushort typeCode)
    {
        var buffer = new List<byte>(32);

        Append16(buffer, transactionId);
        Append16(buffer, 0x0100); // Standard query, recursion desired.
        Append16(buffer, 1); // QDCOUNT
        Append16(buffer, 0); // ANCOUNT
        Append16(buffer, 0); // NSCOUNT
        Append16(buffer, 0); // ARCOUNT

        foreach (var label in name.Trim('.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            buffer.Add((byte)bytes.Length);
            buffer.AddRange(bytes);
        }

        buffer.Add(0); // Root label.
        Append16(buffer, typeCode); // QTYPE
        Append16(buffer, 1); // QCLASS = IN

        return [.. buffer];
    }

    /// <summary>
    /// Parses a response packet into its answer records. Exposed for testing.
    /// </summary>
    internal static IReadOnlyList<Record> ParseResponse(byte[] data, ushort expectedTransactionId)
    {
        if (data.Length < 12)
        {
            throw new DnsResolutionException("Response too short to contain a DNS header.");
        }

        var transactionId = ReadUInt16(data, 0);
        if (transactionId != expectedTransactionId)
        {
            throw new DnsResolutionException("Response transaction id did not match the query.");
        }

        var flags = ReadUInt16(data, 2);
        var responseCode = flags & 0x000F;
        if (responseCode != 0)
        {
            throw new DnsResolutionException(DescribeResponseCode(responseCode));
        }

        var questionCount = ReadUInt16(data, 4);
        var answerCount = ReadUInt16(data, 6);

        var offset = 12;
        for (var i = 0; i < questionCount; i++)
        {
            SkipName(data, ref offset);
            offset += 4; // QTYPE + QCLASS
        }

        var records = new List<Record>(answerCount);
        for (var i = 0; i < answerCount; i++)
        {
            var name = ReadName(data, ref offset);
            var type = ReadUInt16(data, offset);
            offset += 2;
            offset += 2; // CLASS
            var ttl = ReadUInt32(data, offset);
            offset += 4;
            var rdLength = ReadUInt16(data, offset);
            offset += 2;
            var rdataStart = offset;

            var typeName = TypeNameOf(type);
            var value = DecodeRData(data, type, rdataStart, rdLength);
            records.Add(new Record(name, typeName, value, ttl));

            offset = rdataStart + rdLength;
        }

        return records;
    }

    private static string DecodeRData(byte[] data, ushort type, int rdataStart, int rdLength) => type switch
    {
        1 => new IPAddress(data.AsSpan(rdataStart, 4)).ToString(),
        28 => new IPAddress(data.AsSpan(rdataStart, 16)).ToString(),
        5 or 2 or 12 => ReadNameAt(data, rdataStart),
        15 => DecodeMx(data, rdataStart),
        16 => DecodeTxt(data, rdataStart, rdLength),
        6 => DecodeSoa(data, rdataStart),
        33 => DecodeSrv(data, rdataStart),
        _ => Convert.ToHexString(data, rdataStart, rdLength),
    };

    private static string ReadNameAt(byte[] data, int offset)
    {
        var cursor = offset;
        return ReadName(data, ref cursor);
    }

    private static string DecodeMx(byte[] data, int offset)
    {
        var preference = ReadUInt16(data, offset);
        var exchange = ReadNameAt(data, offset + 2);
        return $"{preference.ToString(CultureInfo.InvariantCulture)} {exchange}";
    }

    private static string DecodeSrv(byte[] data, int offset)
    {
        var priority = ReadUInt16(data, offset);
        var weight = ReadUInt16(data, offset + 2);
        var port = ReadUInt16(data, offset + 4);
        var target = ReadNameAt(data, offset + 6);
        return $"{priority.ToString(CultureInfo.InvariantCulture)} {weight.ToString(CultureInfo.InvariantCulture)} {port.ToString(CultureInfo.InvariantCulture)} {target}";
    }

    private static string DecodeSoa(byte[] data, int offset)
    {
        var cursor = offset;
        var mname = ReadName(data, ref cursor);
        var rname = ReadName(data, ref cursor);
        var serial = ReadUInt32(data, cursor);
        var refresh = ReadUInt32(data, cursor + 4);
        var retry = ReadUInt32(data, cursor + 8);
        var expire = ReadUInt32(data, cursor + 12);
        var minimum = ReadUInt32(data, cursor + 16);
        return $"{mname} {rname} {serial} {refresh} {retry} {expire} {minimum}";
    }

    private static string DecodeTxt(byte[] data, int offset, int rdLength)
    {
        var parts = new List<string>();
        var end = offset + rdLength;
        var cursor = offset;
        while (cursor < end)
        {
            var length = data[cursor];
            cursor++;
            parts.Add(Encoding.ASCII.GetString(data, cursor, Math.Min(length, end - cursor)));
            cursor += length;
        }

        return string.Join(" ", parts);
    }

    private static string TypeNameOf(ushort type) =>
        TypeCodes.FirstOrDefault(kv => kv.Value == type).Key ?? type.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a (possibly compressed) domain name starting at <paramref name="offset"/>, advancing it past the name.
    /// </summary>
    private static string ReadName(byte[] data, ref int offset)
    {
        var labels = new List<string>();
        var jumped = false;
        var cursor = offset;
        var safety = 0;

        while (safety++ < 128)
        {
            var length = data[cursor];
            if (length == 0)
            {
                cursor++;
                break;
            }

            if ((length & 0xC0) == 0xC0)
            {
                var pointer = ((length & 0x3F) << 8) | data[cursor + 1];
                if (!jumped)
                {
                    offset = cursor + 2;
                    jumped = true;
                }

                cursor = pointer;
                continue;
            }

            labels.Add(Encoding.ASCII.GetString(data, cursor + 1, length));
            cursor += 1 + length;
        }

        if (!jumped)
        {
            offset = cursor;
        }

        return string.Join('.', labels);
    }

    private static void SkipName(byte[] data, ref int offset)
    {
        while (true)
        {
            var length = data[offset];
            if (length == 0)
            {
                offset++;
                return;
            }

            if ((length & 0xC0) == 0xC0)
            {
                offset += 2;
                return;
            }

            offset += 1 + length;
        }
    }

    private static void Append16(List<byte> buffer, int value)
    {
        buffer.Add((byte)(value >> 8));
        buffer.Add((byte)value);
    }

    private static ushort ReadUInt16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));

    private static uint ReadUInt32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));

    private static string DescribeResponseCode(int code) => code switch
    {
        1 => "The DNS server reported a format error in the query.",
        2 => "The DNS server failed to process the query (server failure).",
        3 => "The name does not exist (NXDOMAIN).",
        4 => "The DNS server does not support that query type.",
        5 => "The DNS server refused the query.",
        _ => $"The DNS server returned response code {code}.",
    };

    /// <summary>
    /// Converts an IP address to its reverse lookup name, e.g. 1.2.3.4 -&gt; 4.3.2.1.in-addr.arpa.
    /// </summary>
    internal static string ToReverseName(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            Array.Reverse(bytes);
            return string.Join('.', bytes.Select(b => b.ToString(CultureInfo.InvariantCulture))) + ".in-addr.arpa";
        }

        var nibbles = new List<string>();
        var reversedBytes = address.GetAddressBytes();
        Array.Reverse(reversedBytes);
        foreach (var b in reversedBytes)
        {
            nibbles.Add((b & 0x0F).ToString("x", CultureInfo.InvariantCulture));
            nibbles.Add((b >> 4).ToString("x", CultureInfo.InvariantCulture));
        }

        return string.Join('.', nibbles) + ".ip6.arpa";
    }
}

/// <summary>
/// Raised when a DNS server returns a valid response with an error code, such as NXDOMAIN.
/// </summary>
public sealed class DnsResolutionException(string message) : Exception(message);
