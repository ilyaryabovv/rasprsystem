using System.Text.Json;
using FileService.Storage;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FileService.Queue;

public class ReportFileConsumer : BackgroundService
{
    private readonly RabbitOptions _settings;
    private readonly IFileStorage _storage;
    private readonly ILogger<ReportFileConsumer> _logger;

    public ReportFileConsumer(IOptions<RabbitOptions> options, IFileStorage storage, ILogger<ReportFileConsumer> logger)
    {
        _settings = options.Value;
        _storage = storage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.User,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(_settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var gate = new SemaphoreSlim(1, 1);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, delivery) =>
                {
                    await gate.WaitAsync(stoppingToken);
                    try
                    {
                        await Handle(delivery.Body);
                        channel.BasicAck(delivery.DeliveryTag, multiple: false);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Failed to store report file");
                        channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
                    }
                    finally
                    {
                        gate.Release();
                    }
                };

                channel.BasicConsume(_settings.Queue, autoAck: false, consumer);
                _logger.LogInformation("Listening for report files on {Queue}", _settings.Queue);

                while (connection.IsOpen && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "RabbitMQ connection failed, retrying");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task Handle(ReadOnlyMemory<byte> body)
    {
        ReportFileMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ReportFileMessage>(body.Span);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Skipped report message with invalid JSON");
            return;
        }

        if (message is null || !IsSafeName(message.FileName))
        {
            _logger.LogWarning("Skipped report message with an invalid file name");
            return;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(message.Content);
        }
        catch (FormatException)
        {
            _logger.LogWarning("Skipped report message with invalid content");
            return;
        }

        await using var stream = new MemoryStream(bytes);
        await _storage.Save(message.FileName, stream);
        _logger.LogInformation("Stored report file {FileName}", message.FileName);
    }

    private static bool IsSafeName(string name)
    {
        return name.Length > 0
            && name.Length <= 120
            && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_');
    }
}
