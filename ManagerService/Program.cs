using System.Diagnostics;
using System.Text;
using ManagerService.Auth;
using ManagerService.Clients;
using ManagerService.Data;
using ManagerService.Discovery;
using ManagerService.Logging;
using ManagerService.Saga;
using ManagerService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using Serilog;
using ManagerSvc = ManagerService.Services.ManagerService;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.With<TraceEnricher>()
        .Enrich.WithProperty("Service", "manager")
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

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();
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
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<ExportSagaService>();
builder.Services.AddSingleton<IReportPublisher>(sp =>
    ReportPublisher.Connect(
        sp.GetRequiredService<IOptions<RabbitOptions>>().Value,
        sp.GetRequiredService<ILogger<ReportPublisher>>()));
builder.Services.AddHostedService<ReportSagaConsumer>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Manager Service", Version = "v1" });
});

var instanceName = string.IsNullOrWhiteSpace(builder.Configuration["Instance:Name"])
    ? Environment.MachineName
    : builder.Configuration["Instance:Name"]!;
var app = builder.Build();

app.Services.GetRequiredService<IReportPublisher>();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Instance"] = instanceName;
        return Task.CompletedTask;
    });
    await next();
});
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
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
