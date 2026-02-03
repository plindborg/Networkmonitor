using System.Text.Json;
using System.Text.Json.Nodes;
using NetworkMonitor.Options;
using SharpPcap;

namespace NetworkMonitor.Services;

public class DeviceConfigService
{
    private readonly string _settingsPath;

    public DeviceConfigService(IHostEnvironment environment)
    {
        _settingsPath = Path.Combine(environment.ContentRootPath, "appsettings.json");
    }

    public IEnumerable<object> GetNetworkDevices()
    {
        var devices = CaptureDeviceList.Instance;
        return devices.Select(device => new
        {
            name = device.Name,
            description = device.Description ?? string.Empty
        });
    }

    public async Task<bool> UpdateDeviceNameAsync(string deviceName, CancellationToken cancellationToken)
    {
        try
        {
            var root = await LoadSettingsAsync(cancellationToken);
            var monitorNode = root[MonitorOptions.SectionName] as JsonObject ?? new JsonObject();
            monitorNode["DeviceName"] = deviceName;
            root[MonitorOptions.SectionName] = monitorNode;

            await File.WriteAllTextAsync(
                _settingsPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<JsonObject> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return new JsonObject();
        }

        var json = await File.ReadAllTextAsync(_settingsPath, cancellationToken);
        var node = JsonNode.Parse(json) as JsonObject;
        return node ?? new JsonObject();
    }
}
