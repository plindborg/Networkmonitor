using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Models;
using NetworkMonitor.Options;
using PacketDotNet;

namespace NetworkMonitor.Services;

public class TrafficAnalyzer
{
    private readonly ILogger<TrafficAnalyzer> _logger;
    private readonly MonitorOptions _options;

    public TrafficAnalyzer(ILogger<TrafficAnalyzer> logger, IOptions<MonitorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public TrafficEvent? Analyze(Packet packet)
    {
        var ipPacket = packet.Extract<IPPacket>();
        if (ipPacket is null)
        {
            return null;
        }

        var protocol = ipPacket.Protocol.ToString();
        int? destinationPort = null;
        string? hostname = null;
        string? url = null;

        var tcpPacket = packet.Extract<TcpPacket>();
        if (tcpPacket is not null)
        {
            destinationPort = tcpPacket.DestinationPort;
            protocol = "TCP";
            if (tcpPacket.PayloadData?.Length > 0)
            {
                (hostname, url) = ExtractHttpDetails(tcpPacket.PayloadData);
            }
        }

        var udpPacket = packet.Extract<UdpPacket>();
        if (udpPacket is not null)
        {
            destinationPort = udpPacket.DestinationPort;
            protocol = "UDP";
        }

        var bytes = ipPacket.TotalLength;

        return new TrafficEvent(
            DateTimeOffset.UtcNow,
            ipPacket.SourceAddress.ToString(),
            ipPacket.DestinationAddress.ToString(),
            protocol,
            destinationPort,
            hostname,
            url,
            bytes);
    }

    private (string? Hostname, string? Url) ExtractHttpDetails(byte[] payload)
    {
        try
        {
            var maxBytes = Math.Min(payload.Length, _options.MaxPayloadBytes);
            var text = Encoding.ASCII.GetString(payload, 0, maxBytes);
            if (!text.Contains("HTTP", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            string? hostname = null;
            string? url = null;

            var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                var requestLine = lines[0].Split(' ');
                if (requestLine.Length >= 2)
                {
                    url = requestLine[1];
                }
            }

            foreach (var line in lines)
            {
                if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                {
                    hostname = line[5..].Trim();
                    break;
                }
            }

            return (hostname, url);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse HTTP payload.");
            return (null, null);
        }
    }
}
