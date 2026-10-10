using ex1.Exceptions;
using ex1.Lottery;
using FluentAssertions;

namespace TestProject1.Lottery;

public class WeightedWinnerPickerTests
{
    /// <summary>Returns a fixed roll so each test controls exactly which ticket is drawn.</summary>
    private sealed class FixedRandom(int roll) : IRandomSource
    {
        public int? LastMax { get; private set; }

        public int NextInt(int maxExclusive)
        {
            LastMax = maxExclusive;
            return roll;
        }
    }

    private static RaffleEntry Entry(int userId, int tickets) =>
        new(userId, $"First{userId}", $"Last{userId}", $"user{userId}@test.com", tickets);

    private static readonly IReadOnlyList<RaffleEntry> AliceOneBobThree = [Entry(1, 1), Entry(2, 3)];

    [Fact]
    public void Rolls_over_the_total_number_of_tickets()
    {
        var random = new FixedRandom(0);

        new WeightedWinnerPicker(random).Pick(AliceOneBobThree);

        random.LastMax.Should().Be(4);
    }

    [Theory]
    [InlineData(0, 1)] // ticket 0 belongs to user 1
    [InlineData(1, 2)] // tickets 1..3 belong to user 2
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void Maps_each_roll_to_the_owner_of_that_ticket(int roll, int expectedUserId)
    {
        var winner = new WeightedWinnerPicker(new FixedRandom(roll)).Pick(AliceOneBobThree);

        winner.UserId.Should().Be(expectedUserId);
    }

    [Fact]
    public void Entries_with_no_tickets_can_never_win()
    {
        var entries = new[] { Entry(1, 0), Entry(2, 2), Entry(3, 0) };

        for (var roll = 0; roll < 2; roll++)
            new WeightedWinnerPicker(new FixedRandom(roll)).Pick(entries).UserId.Should().Be(2);
    }

    [Fact]
    public void Throws_when_there_are_no_tickets()
    {
        var act = () => new WeightedWinnerPicker(new FixedRandom(0)).Pick([Entry(1, 0)]);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Throws_when_there_are_no_entries()
    {
        var act = () => new WeightedWinnerPicker(new FixedRandom(0)).Pick([]);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Win_rate_is_proportional_to_tickets_held_with_real_randomness()
    {
        var picker = new WeightedWinnerPicker(new CryptoRandomSource());
        const int draws = 40_000;

        var bobWins = Enumerable.Range(0, draws).Count(_ => picker.Pick(AliceOneBobThree).UserId == 2);

        // Bob holds 3 of 4 tickets, so should win ~75% of draws
        ((double)bobWins / draws).Should().BeApproximately(0.75, 0.02);
    }
}
