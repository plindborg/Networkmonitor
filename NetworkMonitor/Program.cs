using Microsoft.Extensions.Options;
using NetworkMonitor.Data;
using NetworkMonitor.Options;
using NetworkMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "NETWORKMONITOR_");

builder.Services.Configure<MonitorOptions>(builder.Configuration.GetSection(MonitorOptions.SectionName));

builder.Services.AddSingleton<TrafficDatabase>();
builder.Services.AddSingleton<TrafficAnalyzer>();
builder.Services.AddSingleton<WebsiteStatisticsService>();
builder.Services.AddSingleton<DeviceConfigService>();
builder.Services.AddSingleton<IPacketSource, SharpPcapPacketSource>();
builder.Services.AddHostedService<PacketCaptureService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

await app.Services.GetRequiredService<TrafficDatabase>().InitializeAsync();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/devices", (DeviceConfigService deviceConfigService) =>
    Results.Ok(deviceConfigService.GetNetworkDevices()));

app.MapGet("/api/config", (IOptionsMonitor<MonitorOptions> options) =>
    Results.Ok(new { deviceName = options.CurrentValue.DeviceName ?? string.Empty }));

app.MapPost("/api/device", async (
    DeviceSelectionRequest request,
    DeviceConfigService deviceConfigService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceName))
    {
        return Results.BadRequest(new { message = "DeviceName saknas." });
    }

    var updated = await deviceConfigService.UpdateDeviceNameAsync(request.DeviceName, cancellationToken);
    return Results.Ok(new
    {
        message = updated
            ? "Nätverkskort uppdaterat. Starta om tjänsten för att byta capture-enhet."
            : "Kunde inte uppdatera nätverkskortet."
    });
});

app.Run();

internal record DeviceSelectionRequest(string DeviceName);
