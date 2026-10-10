using FluentAssertions;
using Lottery.Messaging.Contracts;
using Lottery.Messaging.Email;
using Lottery.NotificationWorker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestProject1.Worker;

public class WinnerNotificationHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly WinnerNotificationHandler _handler;

    public WinnerNotificationHandlerTests()
    {
        _handler = new WinnerNotificationHandler(_email.Object, NullLogger<WinnerNotificationHandler>.Instance);
    }

    private static WinnerDrawnEvent Event(string email = "alice@test.com") =>
        new(1, "Bike", 10, "Alice Smith", email, DateTime.UtcNow);

    [Fact]
    public async Task Emails_the_winner_about_their_prize()
    {
        await _handler.HandleAsync(Event());

        _email.Verify(e => e.SendWinnerEmail("alice@test.com", "Alice Smith", "Bike"), Times.Once);
    }

    [Fact]
    public async Task Rejects_a_message_without_an_email_address()
    {
        var act = () => _handler.HandleAsync(Event(email: ""));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Lets_email_failures_propagate_so_the_message_is_dead_lettered()
    {
        _email.Setup(e => e.SendWinnerEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var act = () => _handler.HandleAsync(Event());

        await act.Should().ThrowAsync<Exception>().WithMessage("SMTP down");
    }
}
