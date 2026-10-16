using FileService.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<SiloOptions>(builder.Configuration.GetSection("Silo"));
builder.Services.AddSingleton<IFileStorage, SiloStorage>();

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

app.MapControllers();
app.Run();
