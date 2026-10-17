using FileService.Queue;
using FileService.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<SiloOptions>(builder.Configuration.GetSection("Silo"));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IFileStorage, SiloStorage>();
builder.Services.AddHostedService<ReportFileConsumer>();

var app = builder.Build();

var storage = app.Services.GetRequiredService<IFileStorage>();
for (var attempt = 1; ; attempt++)
{
    try
    {
        await storage.EnsureBucket();
        break;
    }
    catch (Exception) when (attempt < 10)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

var instanceName = Environment.MachineName;
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Instance"] = instanceName;
        return Task.CompletedTask;
    });
    await next();
});

app.MapControllers();
app.Run();
