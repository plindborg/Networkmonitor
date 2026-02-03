using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Data;
using NetworkMonitor.Options;
using NetworkMonitor.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.AddEnvironmentVariables(prefix: "NETWORKMONITOR_");
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<MonitorOptions>(context.Configuration.GetSection(MonitorOptions.SectionName));

        services.AddSingleton<TrafficDatabase>();
        services.AddSingleton<TrafficAnalyzer>();
        services.AddSingleton<WebsiteStatisticsService>();
        services.AddSingleton<IPacketSource, SharpPcapPacketSource>();
        services.AddHostedService<PacketCaptureService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.Services.GetRequiredService<TrafficDatabase>().InitializeAsync();
await host.RunAsync();
