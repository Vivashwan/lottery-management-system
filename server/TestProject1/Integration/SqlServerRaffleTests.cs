using ex1.Data;
using ex1.DTOs;
using ex1.Lottery;
using ex1.Models;
using ex1.Repository;
using ex1.services;
using FluentAssertions;
using Lottery.Messaging;
using Lottery.Messaging.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestProject1.Integration;

/// <summary>
/// Runs the EF migrations, stored procedures and function against a real SQL Server.
/// Set LOTTERY_TEST_SQL to a server connection string (without a database) to enable, e.g.
///   Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True
/// Each test class run gets its own throwaway database.
/// </summary>
public class SqlServerRaffleTests : IAsyncLifetime
{
    private static readonly string? ServerConnection = Environment.GetEnvironmentVariable("LOTTERY_TEST_SQL");
    private const string SkipReason = "LOTTERY_TEST_SQL is not set; skipping SQL Server integration tests.";

    private string _connectionString = string.Empty;
    private int _prizeId;
    private int _heavyBuyerId;   // 3 paid tickets
    private int _lightBuyerId;   // 1 paid ticket
    private int _unpaidBuyerId;  // 5 tickets in an unpaid cart

    private StoreContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreContext>().UseSqlServer(_connectionString).Options);

    public async Task InitializeAsync()
    {
        if (ServerConnection is null) return;

        _connectionString = new SqlConnectionStringBuilder(ServerConnection)
        {
            InitialCatalog = $"lottery_test_{Guid.NewGuid():N}",
        }.ConnectionString;

        await using var db = NewContext();
        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (ServerConnection is null) return;
        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
    }

    private async Task SeedAsync(StoreContext db)
    {
        var category = new Category { Name = "Sports" };
        var donor = new Donor { FirstName = "Dana", LastName = "Donor", Email = "dana@test.com", Phone = "050" };
        db.AddRange(category, donor);
        await db.SaveChangesAsync();

        var prize = new Prize
        {
            Name = "Bike", Description = "Mountain bike", ImagePath = "bike.png", TicketPrice = 10m,
            CategoryId = category.Id, DonorId = donor.Id,
        };
        var users = new[] { NewUser("Heavy"), NewUser("Light"), NewUser("Unpaid") };
        db.Add(prize);
        db.AddRange(users);
        await db.SaveChangesAsync();

        db.AddRange(
            CartWith(users[0], prize, quantity: 3, CartStatus.Paid),
            CartWith(users[1], prize, quantity: 1, CartStatus.Paid),
            CartWith(users[2], prize, quantity: 5, CartStatus.Draft));
        await db.SaveChangesAsync();

        (_prizeId, _heavyBuyerId, _lightBuyerId, _unpaidBuyerId) = (prize.Id, users[0].Id, users[1].Id, users[2].Id);
    }

    private static User NewUser(string name) => new()
    {
        FirstName = name, LastName = "Buyer", Email = $"{name.ToLower()}@test.com", Phone = "050",
        PasswordHash = "hash", Address = "Street 1", SignAt = DateTime.UtcNow,
    };

    private static Cart CartWith(User user, Prize prize, int quantity, CartStatus status) => new()
    {
        UserId = user.Id,
        Status = status,
        Items = [new CartItem { PrizeId = prize.Id, Quantity = quantity }],
    };

    [SkippableFact]
    public async Task Raffle_entries_are_weighted_by_paid_tickets_only()
    {
        Skip.If(ServerConnection is null, SkipReason);
        await using var db = NewContext();

        var entries = await new LotteryRepository(db).GetRaffleEntries(_prizeId);

        entries.Select(e => (e.UserId, e.TicketCount)).Should().BeEquivalentTo(
            [(_heavyBuyerId, 3), (_lightBuyerId, 1)]);
        entries.Should().NotContain(e => e.UserId == _unpaidBuyerId);
    }

    [SkippableFact]
    public async Task Ticket_count_function_ignores_unpaid_carts()
    {
        Skip.If(ServerConnection is null, SkipReason);
        await using var db = NewContext();

        var count = await db.Database
            .SqlQuery<int>($"SELECT dbo.fn_PrizeTicketCount({_prizeId}) AS [Value]")
            .SingleAsync();

        count.Should().Be(4);
    }

    [SkippableFact]
    public async Task Winner_can_only_be_recorded_once()
    {
        Skip.If(ServerConnection is null, SkipReason);
        await using var db = NewContext();
        var repo = new LotteryRepository(db);

        (await repo.TryRecordWinner(_prizeId, _heavyBuyerId)).Should().BeTrue();
        (await repo.TryRecordWinner(_prizeId, _lightBuyerId)).Should().BeFalse();

        var prize = await db.Prizes.AsNoTracking().SingleAsync(p => p.Id == _prizeId);
        prize.IsRaffleDone.Should().BeTrue();
        prize.WinnerUserId.Should().Be(_heavyBuyerId);
    }

    [SkippableFact]
    public async Task Only_one_of_many_concurrent_draws_succeeds()
    {
        Skip.If(ServerConnection is null, SkipReason);

        // 20 draws race on separate connections, as 20 simultaneous HTTP requests would
        var attempts = Enumerable.Range(0, 20).Select(async i =>
        {
            var userId = i % 2 == 0 ? _heavyBuyerId : _lightBuyerId;
            await using var db = NewContext();
            return (userId, won: await new LotteryRepository(db).TryRecordWinner(_prizeId, userId));
        });
        var results = await Task.WhenAll(attempts);

        results.Count(r => r.won).Should().Be(1);

        await using var check = NewContext();
        var prize = await check.Prizes.AsNoTracking().SingleAsync(p => p.Id == _prizeId);
        prize.WinnerUserId.Should().Be(results.Single(r => r.won).userId);
    }

    [SkippableFact]
    public async Task Report_shows_tickets_revenue_and_winner()
    {
        Skip.If(ServerConnection is null, SkipReason);
        await using var db = NewContext();
        var repo = new LotteryRepository(db);
        await repo.TryRecordWinner(_prizeId, _lightBuyerId);

        List<RaffleReportRowDto> report = await repo.GetRaffleReport();

        var row = report.Single(r => r.PrizeId == _prizeId);
        row.TicketsSold.Should().Be(4);
        row.Revenue.Should().Be(40.00m);
        row.IsRaffleDone.Should().BeTrue();
        row.WinnerName.Should().Be("Light Buyer");
    }

    [SkippableFact]
    public async Task Full_draw_records_a_paying_winner_and_queues_the_notification()
    {
        Skip.If(ServerConnection is null, SkipReason);
        await using var db = NewContext();
        var publisher = new Mock<IMessagePublisher>();
        var service = new LotteryService(new LotteryRepository(db),
            new WeightedWinnerPicker(new CryptoRandomSource()), publisher.Object,
            NullLogger<LotteryService>.Instance);

        var result = await service.RafflePrizeAsync(_prizeId);

        result.WinnerUserId.Should().BeOneOf(_heavyBuyerId, _lightBuyerId);
        publisher.Verify(p => p.PublishAsync(
            It.Is<WinnerDrawnEvent>(e => e.WinnerUserId == result.WinnerUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
