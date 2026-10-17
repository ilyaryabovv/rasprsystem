using Gateway.Discovery;

var builder = WebApplication.CreateBuilder(args);

var discoveryName = builder.Configuration["Discovery:ServiceName"];
var proxy = builder.Services.AddReverseProxy();

if (string.IsNullOrWhiteSpace(discoveryName))
{
    proxy.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
}
else
{
    builder.Services.AddSingleton<InMemoryConfigProvider>();
    builder.Services.AddSingleton<Yarp.ReverseProxy.Configuration.IProxyConfigProvider>(
        provider => provider.GetRequiredService<InMemoryConfigProvider>());
    builder.Services.AddHostedService<DestinationDiscoveryService>();
}

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(discoveryName))
{
    app.MapGet("/gateway/destinations", (InMemoryConfigProvider config) => config.Addresses);
}

app.MapReverseProxy();
app.Run();
