namespace Lottery.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string QueueName { get; set; } = "winner-notifications";
    public string DeadLetterQueueName { get; set; } = "winner-notifications.dead-letter";

    /// <summary>How many unacknowledged messages a consumer may hold at once.</summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>How many messages a consumer processes in parallel.</summary>
    public ushort ConsumerConcurrency { get; set; } = 4;
}
