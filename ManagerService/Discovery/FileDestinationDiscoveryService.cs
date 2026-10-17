using System.Net;
using System.Net.Sockets;

namespace ManagerService.Discovery;

public class FileDestinationDiscoveryService : BackgroundService
{
    private readonly FileInstanceRegistry _registry;
    private readonly string _serviceName;
    private readonly ILogger<FileDestinationDiscoveryService> _logger;

    public FileDestinationDiscoveryService(
        FileInstanceRegistry registry,
        IConfiguration configuration,
        ILogger<FileDestinationDiscoveryService> logger)
    {
        _registry = registry;
        _serviceName = configuration["Discovery:ServiceName"] ?? "files";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var previous = "";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(_serviceName, stoppingToken);
                var ips = addresses
                    .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(address => address.ToString())
                    .Distinct()
                    .Order()
                    .ToArray();

                var current = string.Join(",", ips);
                if (ips.Length > 0 && current != previous)
                {
                    _registry.Update(ips);
                    previous = current;
                    _logger.LogInformation("File destinations updated: {Addresses}", current);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Could not resolve service {ServiceName}", _serviceName);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
