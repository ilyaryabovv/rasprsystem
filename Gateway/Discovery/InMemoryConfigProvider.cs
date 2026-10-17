using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Discovery;

public class InMemoryConfigProvider : IProxyConfigProvider
{
    private volatile Config _config;

    public InMemoryConfigProvider()
    {
        _config = new Config([], []);
    }

    public IReadOnlyList<string> Addresses { get; private set; } = [];

    public IProxyConfig GetConfig() => _config;

    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, IReadOnlyList<string> addresses)
    {
        var old = _config;
        Addresses = addresses;
        _config = new Config(routes, clusters);
        old.SignalChange();
    }

    private sealed class Config : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public Config(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public void SignalChange() => _cts.Cancel();
    }
}
