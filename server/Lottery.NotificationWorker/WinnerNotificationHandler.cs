using Lottery.Messaging.Contracts;
using Lottery.Messaging.Email;

namespace Lottery.NotificationWorker;

/// <summary>Business logic for one message, kept separate from RabbitMQ plumbing so it can be unit-tested.</summary>
public sealed class WinnerNotificationHandler(IEmailService emailService, ILogger<WinnerNotificationHandler> logger)
{
    public async Task HandleAsync(WinnerDrawnEvent message)
    {
        if (string.IsNullOrWhiteSpace(message.WinnerEmail))
            throw new InvalidOperationException($"Winner of prize {message.PrizeId} has no email address.");

        await emailService.SendWinnerEmail(message.WinnerEmail, message.WinnerName, message.PrizeName);

        logger.LogInformation("Notified winner {UserId} of prize {PrizeId}", message.WinnerUserId, message.PrizeId);
    }
}
