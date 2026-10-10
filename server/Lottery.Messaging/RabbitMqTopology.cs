using RabbitMQ.Client;

namespace Lottery.Messaging;

/// <summary>
/// Declares the durable work queue and its dead-letter queue. Declarations are idempotent,
/// so both the publisher and the consumer call this and whichever starts first creates them.
/// </summary>
public static class RabbitMqTopology
{
    public static async Task DeclareAsync(IChannel channel, RabbitMqOptions options, CancellationToken ct = default)
    {
        // Messages a consumer rejects (after failing to process) are routed here instead of being lost
        await channel.QueueDeclareAsync(options.DeadLetterQueueName, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);

        await channel.QueueDeclareAsync(options.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = options.DeadLetterQueueName,
            },
            cancellationToken: ct);
    }

    public static ConnectionFactory CreateConnectionFactory(RabbitMqOptions options) => new()
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        ConsumerDispatchConcurrency = options.ConsumerConcurrency,
        AutomaticRecoveryEnabled = true,
    };
}
