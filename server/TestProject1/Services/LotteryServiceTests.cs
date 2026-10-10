using ex1.DTOs;
using ex1.Exceptions;
using ex1.Interfaces.Repositorys;
using ex1.Lottery;
using ex1.Models;
using ex1.services;
using FluentAssertions;
using Lottery.Messaging;
using Lottery.Messaging.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestProject1.Services;

public class LotteryServiceTests
{
    private readonly Mock<ILotteryRepository> _repo = new();
    private readonly Mock<IWinnerPicker> _picker = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly LotteryService _service;

    private static readonly RaffleEntry Alice = new(10, "Alice", "Smith", "alice@test.com", 3);
    private static readonly RaffleEntry Bob = new(11, "Bob", "Jones", "bob@test.com", 1);

    public LotteryServiceTests()
    {
        _service = new LotteryService(_repo.Object, _picker.Object, _publisher.Object,
            NullLogger<LotteryService>.Instance);
    }

    private void GivenOpenPrizeWithEntries(params RaffleEntry[] entries)
    {
        _repo.Setup(r => r.GetPrizeById(1)).ReturnsAsync(new Prize { Id = 1, Name = "Bike", IsRaffleDone = false });
        _repo.Setup(r => r.GetRaffleEntries(1)).ReturnsAsync(entries.ToList());
    }

    [Fact]
    public async Task Throws_NotFound_when_prize_does_not_exist()
    {
        _repo.Setup(r => r.GetPrizeById(1)).ReturnsAsync((Prize?)null);

        var act = () => _service.RafflePrizeAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Conflict_when_prize_was_already_drawn()
    {
        _repo.Setup(r => r.GetPrizeById(1)).ReturnsAsync(new Prize { Id = 1, IsRaffleDone = true });

        var act = () => _service.RafflePrizeAsync(1);

        await act.Should().ThrowAsync<ConflictException>();
        _repo.Verify(r => r.GetRaffleEntries(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Throws_BusinessRule_when_nobody_bought_tickets()
    {
        GivenOpenPrizeWithEntries();

        var act = () => _service.RafflePrizeAsync(1);

        await act.Should().ThrowAsync<BusinessRuleException>();
        _repo.Verify(r => r.TryRecordWinner(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Records_the_picked_winner_and_queues_a_notification()
    {
        GivenOpenPrizeWithEntries(Alice, Bob);
        _picker.Setup(p => p.Pick(It.IsAny<IReadOnlyList<RaffleEntry>>())).Returns(Alice);
        _repo.Setup(r => r.TryRecordWinner(1, Alice.UserId)).ReturnsAsync(true);

        LotteryResultDto result = await _service.RafflePrizeAsync(1);

        result.WinnerUserId.Should().Be(Alice.UserId);
        result.Winner!.FullName.Should().Be("Alice Smith");
        result.Winner.Email.Should().Be("alice@test.com");
        result.NotificationQueued.Should().BeTrue();
        result.EmailSent.Should().BeTrue("existing clients still read emailSent");

        _publisher.Verify(p => p.PublishAsync(
            It.Is<WinnerDrawnEvent>(e =>
                e.PrizeId == 1 && e.PrizeName == "Bike" && e.WinnerUserId == Alice.UserId &&
                e.WinnerName == "Alice Smith" && e.WinnerEmail == "alice@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Passes_every_entry_with_its_ticket_count_to_the_picker()
    {
        GivenOpenPrizeWithEntries(Alice, Bob);
        _picker.Setup(p => p.Pick(It.IsAny<IReadOnlyList<RaffleEntry>>())).Returns(Bob);
        _repo.Setup(r => r.TryRecordWinner(1, Bob.UserId)).ReturnsAsync(true);

        await _service.RafflePrizeAsync(1);

        _picker.Verify(p => p.Pick(It.Is<IReadOnlyList<RaffleEntry>>(entries =>
            entries.Count == 2 && entries.Sum(e => e.TicketCount) == 4)));
    }

    [Fact]
    public async Task Throws_Conflict_and_sends_nothing_when_a_concurrent_draw_won_the_race()
    {
        GivenOpenPrizeWithEntries(Alice);
        _picker.Setup(p => p.Pick(It.IsAny<IReadOnlyList<RaffleEntry>>())).Returns(Alice);
        _repo.Setup(r => r.TryRecordWinner(1, Alice.UserId)).ReturnsAsync(false);

        var act = () => _service.RafflePrizeAsync(1);

        await act.Should().ThrowAsync<ConflictException>();
        _publisher.Verify(p => p.PublishAsync(It.IsAny<WinnerDrawnEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Still_returns_the_winner_when_the_message_broker_is_down()
    {
        GivenOpenPrizeWithEntries(Alice);
        _picker.Setup(p => p.Pick(It.IsAny<IReadOnlyList<RaffleEntry>>())).Returns(Alice);
        _repo.Setup(r => r.TryRecordWinner(1, Alice.UserId)).ReturnsAsync(true);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<WinnerDrawnEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("broker unreachable"));

        var result = await _service.RafflePrizeAsync(1);

        result.WinnerUserId.Should().Be(Alice.UserId);
        result.NotificationQueued.Should().BeFalse();
    }
}
