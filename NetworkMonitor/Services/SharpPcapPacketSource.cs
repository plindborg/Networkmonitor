using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetworkMonitor.Options;
using SharpPcap;

namespace NetworkMonitor.Services;

public class SharpPcapPacketSource : IPacketSource
{
    private readonly ILogger<SharpPcapPacketSource> _logger;
    private readonly IOptionsMonitor<MonitorOptions> _options;

    public SharpPcapPacketSource(ILogger<SharpPcapPacketSource> logger, IOptionsMonitor<MonitorOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async IAsyncEnumerable<RawCapture> CaptureAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var device = ResolveDevice();
        if (device is null)
        {
            _logger.LogWarning("No capture device found. Ensure SharpPcap can access your network adapters.");
            yield break;
        }

        var queue = new BlockingCollection<RawCapture>(boundedCapacity: 4096);

        void Handler(object sender, PacketCapture e)
        {
            if (!queue.TryAdd(e.GetPacket()))
            {
                _logger.LogWarning("Packet queue full, dropping packet.");
            }
        }

        device.OnPacketArrival += Handler;
        device.Open(DeviceModes.Promiscuous, read_timeout: 1000);
        device.StartCapture();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                RawCapture? capture = null;
                try
                {
                    capture = queue.Take(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (capture is not null)
                {
                    yield return capture;
                }

                await Task.Yield();
            }
        }
        finally
        {
            device.OnPacketArrival -= Handler;
            device.StopCapture();
            device.Close();
            queue.Dispose();
        }
    }

    private ICaptureDevice? ResolveDevice()
    {
        var devices = CaptureDeviceList.Instance;
        if (devices.Count == 0)
        {
            return null;
        }

        var deviceName = _options.CurrentValue.DeviceName;
        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            var match = devices.FirstOrDefault(device =>
                device.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ||
                device.Description?.Contains(deviceName, StringComparison.OrdinalIgnoreCase) == true);

            if (match is not null)
            {
                _logger.LogInformation("Using capture device {Device}", match.Description ?? match.Name);
                return match;
            }

            _logger.LogWarning("Device {Device} not found, falling back to first available.", deviceName);
        }

        var fallback = devices[0];
        _logger.LogInformation("Using capture device {Device}", fallback.Description ?? fallback.Name);
        return fallback;
    }
}
