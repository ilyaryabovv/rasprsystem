using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using ReportService.Data;
using ReportService.Logging;
using ReportService.Services.Interfaces;
using Serilog;
using ReportSvc = ReportService.Services.ReportService;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.With<TraceEnricher>()
        .Enrich.WithProperty("Service", "report")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {TraceId} {Message:lj}{NewLine}{Exception}");

    var logstashUrl = context.Configuration["Logstash:Url"];
    if (!string.IsNullOrWhiteSpace(logstashUrl))
    {
        logger.WriteTo.Http(logstashUrl, queueLimitBytes: 10_000_000);
    }
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IReportService, ReportSvc>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Report Service", Version = "v1" });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
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
});
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
