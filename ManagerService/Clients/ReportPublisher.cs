using System.Text.Json;
using RabbitMQ.Client;

namespace ManagerService.Clients;

public sealed class ReportPublisher : IReportPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queue;
    private readonly ILogger<ReportPublisher> _logger;
    private readonly object _gate = new();

    private ReportPublisher(IConnection connection, IModel channel, string queue, ILogger<ReportPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _queue = queue;
        _logger = logger;
    }

    public static ReportPublisher Connect(RabbitOptions settings, ILogger<ReportPublisher> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            UserName = settings.User,
            Password = settings.Password
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                channel.QueueDeclare(settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                logger.LogInformation("Connected to RabbitMQ queue {Queue}", settings.Queue);
                return new ReportPublisher(connection, channel, settings.Queue, logger);
            }
            catch (Exception exception) when (attempt < 15)
            {
                logger.LogWarning(exception, "RabbitMQ is not ready, attempt {Attempt}", attempt);
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }

    public Task Publish(byte[] csv, string fileName)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new ReportFileMessage(fileName, Convert.ToBase64String(csv)));
        lock (_gate)
        {
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            _channel.BasicPublish(exchange: "", routingKey: _queue, basicProperties: properties, body: body);
        }

        _logger.LogInformation("Published report file {FileName} to queue {Queue}", fileName, _queue);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
