using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Data;
using PacketDotNet;

namespace NetworkMonitor.Services;

public class PacketCaptureService : BackgroundService
{
    private readonly IPacketSource _packetSource;
    private readonly TrafficAnalyzer _analyzer;
    private readonly TrafficDatabase _database;
    private readonly WebsiteStatisticsService _statisticsService;
    private readonly ILogger<PacketCaptureService> _logger;

    public PacketCaptureService(
        IPacketSource packetSource,
        TrafficAnalyzer analyzer,
        TrafficDatabase database,
        WebsiteStatisticsService statisticsService,
        ILogger<PacketCaptureService> logger)
    {
        _packetSource = packetSource;
        _analyzer = analyzer;
        _database = database;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var rawCapture in _packetSource.CaptureAsync(stoppingToken))
        {
            try
            {
                var packet = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                var trafficEvent = _analyzer.Analyze(packet);
                if (trafficEvent is null)
                {
                    continue;
                }

                await _database.InsertTrafficEventAsync(trafficEvent);
                await _statisticsService.HandleEventAsync(trafficEvent, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze packet.");
            }
        }
    }
}
