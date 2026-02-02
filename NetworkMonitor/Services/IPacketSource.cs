using SharpPcap;

namespace NetworkMonitor.Services;

public interface IPacketSource
{
    IAsyncEnumerable<RawCapture> CaptureAsync(CancellationToken cancellationToken);
}
