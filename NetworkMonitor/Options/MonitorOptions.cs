namespace NetworkMonitor.Options;

public class MonitorOptions
{
    public const string SectionName = "Monitor";

    public string DatabasePath { get; set; } = "network-monitor.db";
    public string? DeviceName { get; set; }
    public int MaxPayloadBytes { get; set; } = 4096;
}
