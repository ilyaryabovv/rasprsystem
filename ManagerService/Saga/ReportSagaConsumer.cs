using System.Text.Json;
using ManagerService.Clients;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ManagerService.Saga;

public class ReportSagaConsumer : BackgroundService
{
    private readonly RabbitOptions _settings;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReportSagaConsumer> _logger;

    public ReportSagaConsumer(IOptions<RabbitOptions> options, IServiceScopeFactory scopes, ILogger<ReportSagaConsumer> logger)
    {
        _settings = options.Value;
        _scopes = scopes;
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
                channel.QueueDeclare(_settings.ResultQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
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
                        _logger.LogError(exception, "Failed to apply saga result");
                        channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
                    }
                    finally
                    {
                        gate.Release();
                    }
                };

                channel.BasicConsume(_settings.ResultQueue, autoAck: false, consumer);
                _logger.LogInformation("Listening for saga results on {Queue}", _settings.ResultQueue);

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
        ReportSagaResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ReportSagaResult>(body.Span);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Skipped saga result with invalid JSON");
            return;
        }

        if (result is null || result.SagaId == Guid.Empty || result.Status is not ("Completed" or "Failed"))
        {
            _logger.LogWarning("Skipped saga result with an invalid status");
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        var sagas = scope.ServiceProvider.GetRequiredService<ExportSagaService>();
        var applied = await sagas.Apply(result.SagaId, result.Status, result.Instance);
        if (!applied)
        {
            _logger.LogWarning("Saga {SagaId} was not found", result.SagaId);
            return;
        }

        _logger.LogInformation("Saga {SagaId} is {Status}, file instance {Instance}", result.SagaId, result.Status, result.Instance);
    }
}
