namespace Lottery.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default);
}
