using Microsoft.EntityFrameworkCore;
using ReportService.Data;
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
        .Enrich.WithProperty("Service", "report")
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
builder.Services.AddScoped<IReportService, ReportSvc>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Report Service", Version = "v1" });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
