using Microsoft.Extensions.Logging;
using NetworkMonitor.Data;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services;

public class WebsiteStatisticsService
{
    private readonly TrafficDatabase _database;
    private readonly ILogger<WebsiteStatisticsService> _logger;

    public WebsiteStatisticsService(TrafficDatabase database, ILogger<WebsiteStatisticsService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task HandleEventAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trafficEvent.Hostname))
        {
            return;
        }

        try
        {
            await _database.UpsertWebsiteStatsAsync(
                trafficEvent.Hostname,
                DateOnly.FromDateTime(trafficEvent.Timestamp.UtcDateTime),
                trafficEvent.Bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update website stats for {Hostname}.", trafficEvent.Hostname);
        }
    }
}
