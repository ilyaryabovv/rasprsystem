using ManagerService.Clients;
using ManagerService.Data;
using ManagerService.Discovery;
using ManagerService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ManagerSvc = ManagerService.Services.ManagerService;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "manager")
        .WriteTo.Console();

    var logstashUrl = context.Configuration["Logstash:Url"];
    if (!string.IsNullOrWhiteSpace(logstashUrl))
    {
        logger.WriteTo.Http(logstashUrl, queueLimitBytes: 10_000_000);
    }
});

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IManagerService, ManagerSvc>();

var reportUrl = builder.Configuration["ReportService:BaseUrl"] ?? "http://localhost:5084";
if (!reportUrl.EndsWith('/'))
{
    reportUrl += "/";
}

builder.Services.AddHttpClient<IReportClient, ReportClient>(client =>
{
    client.BaseAddress = new Uri(reportUrl);
});

var fileUrl = builder.Configuration["FileService:BaseUrl"] ?? "http://localhost:5085";
if (!fileUrl.EndsWith('/'))
{
    fileUrl += "/";
}

builder.Services.AddSingleton(new FileInstanceRegistry(fileUrl));
builder.Services.AddHttpClient<IFileClient, FileClient>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Discovery:ServiceName"]))
{
    builder.Services.AddHostedService<FileDestinationDiscoveryService>();
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Manager Service", Version = "v1" });
});

var instanceName = string.IsNullOrWhiteSpace(builder.Configuration["Instance:Name"])
    ? Environment.MachineName
    : builder.Configuration["Instance:Name"]!;
var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Instance"] = instanceName;
        return Task.CompletedTask;
    });
    await next();
});

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
