using System.Diagnostics;
using Gateway.Discovery;
using OpenTelemetry.Trace;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

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

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request");
    context.Response.OnStarting(() =>
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
        }

        return Task.CompletedTask;
    });
    await next();
    logger.LogInformation(
        "HTTP {Method} {Path} responded {StatusCode} trace {TraceId}",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        Activity.Current?.TraceId.ToString());
});
app.UseHttpMetrics();

if (!string.IsNullOrWhiteSpace(discoveryName))
{
    app.MapGet("/gateway/destinations", (InMemoryConfigProvider config) => config.Addresses);
}

app.MapMetrics();
app.MapReverseProxy();
app.Run();
