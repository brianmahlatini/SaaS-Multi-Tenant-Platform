using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SaaS.Api.Infrastructure.Messaging;

public sealed class RabbitMqConsumerService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("RabbitMQ consumer disabled");
            return Task.CompletedTask;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, args) =>
        {
            var message = Encoding.UTF8.GetString(args.Body.ToArray());
            logger.LogInformation("RabbitMQ event consumed from {QueueName}: {Message}", _options.QueueName, message);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(_options.QueueName, autoAck: false, consumer);
        logger.LogInformation("RabbitMQ consumer started for queue {QueueName}", _options.QueueName);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
