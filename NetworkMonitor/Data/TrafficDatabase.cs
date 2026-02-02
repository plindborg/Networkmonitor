using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NetworkMonitor.Models;
using NetworkMonitor.Options;

namespace NetworkMonitor.Data;

public class TrafficDatabase
{
    private readonly string _connectionString;

    public TrafficDatabase(IOptions<MonitorOptions> options)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.Value.DatabasePath
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var createEvents = @"
CREATE TABLE IF NOT EXISTS traffic_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    source_ip TEXT NOT NULL,
    destination_ip TEXT NOT NULL,
    protocol TEXT NOT NULL,
    destination_port INTEGER,
    hostname TEXT,
    url TEXT,
    bytes INTEGER NOT NULL
);
";

        var createStats = @"
CREATE TABLE IF NOT EXISTS website_stats (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    hostname TEXT NOT NULL,
    date TEXT NOT NULL,
    request_count INTEGER NOT NULL,
    bytes INTEGER NOT NULL,
    UNIQUE(hostname, date)
);
";

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = createEvents;
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = createStats;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task InsertTrafficEventAsync(TrafficEvent trafficEvent)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO traffic_events (
    timestamp,
    source_ip,
    destination_ip,
    protocol,
    destination_port,
    hostname,
    url,
    bytes
) VALUES (
    $timestamp,
    $sourceIp,
    $destinationIp,
    $protocol,
    $destinationPort,
    $hostname,
    $url,
    $bytes
);
";

        command.Parameters.AddWithValue("$timestamp", trafficEvent.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("$sourceIp", trafficEvent.SourceIp);
        command.Parameters.AddWithValue("$destinationIp", trafficEvent.DestinationIp);
        command.Parameters.AddWithValue("$protocol", trafficEvent.Protocol);
        command.Parameters.AddWithValue("$destinationPort", (object?)trafficEvent.DestinationPort ?? DBNull.Value);
        command.Parameters.AddWithValue("$hostname", (object?)trafficEvent.Hostname ?? DBNull.Value);
        command.Parameters.AddWithValue("$url", (object?)trafficEvent.Url ?? DBNull.Value);
        command.Parameters.AddWithValue("$bytes", trafficEvent.Bytes);

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpsertWebsiteStatsAsync(string hostname, DateOnly date, long bytes)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO website_stats (hostname, date, request_count, bytes)
VALUES ($hostname, $date, 1, $bytes)
ON CONFLICT(hostname, date)
DO UPDATE SET
    request_count = request_count + 1,
    bytes = bytes + $bytes;
";

        command.Parameters.AddWithValue("$hostname", hostname);
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$bytes", bytes);

        await command.ExecuteNonQueryAsync();
    }
}
