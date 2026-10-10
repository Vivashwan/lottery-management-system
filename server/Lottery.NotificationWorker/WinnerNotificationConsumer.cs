using System.Text.Json;
using Lottery.Messaging;
using Lottery.Messaging.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lottery.NotificationWorker;

/// <summary>
/// Consumes winner events from RabbitMQ. Messages are acknowledged only after the email is sent,
/// so a crash mid-send redelivers the message instead of losing it. Messages that fail are
/// rejected to the dead-letter queue for inspection rather than retried forever.
/// Up to <see cref="RabbitMqOptions.ConsumerConcurrency"/> messages are processed in parallel.
/// </summary>
public sealed class WinnerNotificationConsumer(
    IOptions<RabbitMqOptions> options,
    WinnerNotificationHandler handler,
    ILogger<WinnerNotificationConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await ConnectWithRetryAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(_channel, _options, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => OnMessageAsync(delivery, stoppingToken);

        await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming {Queue} with concurrency {Concurrency}", _options.QueueName, _options.ConsumerConcurrency);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task OnMessageAsync(BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<WinnerDrawnEvent>(delivery.Body.Span)
                ?? throw new JsonException("Empty message body");

            await handler.HandleAsync(message);
            await _channel!.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message {MessageId}; sending to dead-letter queue",
                delivery.BasicProperties.MessageId);
            await _channel!.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
        }
    }

    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken ct)
    {
        var factory = RabbitMqTopology.CreateConnectionFactory(_options);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await factory.CreateConnectionAsync(ct);
            }
            catch (Exception ex) when (attempt < 12 && !ct.IsCancellationRequested)
            {
                logger.LogWarning("RabbitMQ not reachable (attempt {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
