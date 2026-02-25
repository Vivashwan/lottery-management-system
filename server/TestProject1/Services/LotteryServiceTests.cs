using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Interfaces.Services;
using ex1.DTOs;
using ex1.Models;
namespace TestProject1.Services;

public class LotteryServiceTests
{
    private readonly Mock<ILotteryRepository> _repo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILogger<LotteryService>> _logger = new();
    private readonly LotteryService _service;

    public LotteryServiceTests()
    {
        _service = new LotteryService(_repo.Object, _email.Object, _logger.Object);
    }

    [Fact]
    public async Task RafflePrizeAsync_WhenNoEntries_Throws()
    {
        var entries = new List<User>();
        _repo.Setup(r => r.GetRaffleEntries(1)).ReturnsAsync(entries);

        var act = async () => await _service.RafflePrizeAsync(1);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("אין משתתפים להגרלה");
    }

    [Fact]
    public async Task RafflePrizeAsync_WhenEmailFails_EmailSentFalse()
    {
        var entries = new List<User> 
        {
            new User { Id = 10, FirstName = "A", LastName = "B", Email = "a@a.com" }
        };

        _repo.Setup(r => r.GetRaffleEntries(1)).ReturnsAsync(entries);

        _repo.Setup(r => r.GetPrizeById(1))
            .ReturnsAsync(new Prize { Id = 1, Name = "Prize" });

        _repo.Setup(r => r.SetWinner(1, 10)).Returns(Task.CompletedTask);

        _email.Setup(e => e.SendWinnerEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .ThrowsAsync(new Exception("smtp down"));

      
        LotteryResultDto res = await _service.RafflePrizeAsync(1);

        res.PrizeId.Should().Be(1);
        res.WinnerUserId.Should().Be(10);
        res.EmailSent.Should().BeFalse();

        res.Winner.Should().NotBeNull();
        res.Winner!.Id.Should().Be(10);
        res.Winner.FullName.Should().Be("A B");
        res.Winner.Email.Should().Be("a@a.com");
    }
}
