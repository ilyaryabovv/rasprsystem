using System.Net;
using System.Net.Sockets;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Discovery;

public class DestinationDiscoveryService : BackgroundService
{
    private static readonly RouteConfig ManagersRoute = new()
    {
        RouteId = "managers",
        ClusterId = "managers",
        Match = new RouteMatch { Path = "/api/Manager/{**catch-all}" }
    };

    private readonly InMemoryConfigProvider _config;
    private readonly string _serviceName;
    private readonly ILogger<DestinationDiscoveryService> _logger;

    public DestinationDiscoveryService(
        InMemoryConfigProvider config,
        IConfiguration configuration,
        ILogger<DestinationDiscoveryService> logger)
    {
        _config = config;
        _serviceName = configuration["Discovery:ServiceName"] ?? "api";
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
                    var destinations = ips.ToDictionary(
                        ip => ip,
                        ip => new DestinationConfig { Address = $"http://{ip}:8080/" });

                    _config.Update(
                        [ManagersRoute],
                        [new ClusterConfig
                        {
                            ClusterId = "managers",
                            LoadBalancingPolicy = "Random",
                            Destinations = destinations
                        }],
                        ips);

                    previous = current;
                    _logger.LogInformation("Gateway destinations updated: {Addresses}", current);
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
