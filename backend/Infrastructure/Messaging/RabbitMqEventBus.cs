using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SaaS.Api.Infrastructure.Messaging;

public sealed class RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger) : IEventBus, IDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    public Task PublishAsync(PlatformEvent platformEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("RabbitMQ disabled. Event {EventType} handled locally", platformEvent.Type);
            return Task.CompletedTask;
        }

        EnsureChannel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(platformEvent));
        _channel!.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            basicProperties: null,
            body: body);

        logger.LogInformation("Published {EventType} event to RabbitMQ queue {QueueName}", platformEvent.Type, _options.QueueName);
        return Task.CompletedTask;
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = false
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
