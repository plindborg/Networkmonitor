namespace NetworkMonitor.Models;

public record TrafficEvent(
    DateTimeOffset Timestamp,
    string SourceIp,
    string DestinationIp,
    string Protocol,
    int? DestinationPort,
    string? Hostname,
    string? Url,
    long Bytes);
