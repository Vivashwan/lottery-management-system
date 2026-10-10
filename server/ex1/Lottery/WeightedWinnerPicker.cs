using ex1.Exceptions;

namespace ex1.Lottery;

public interface IWinnerPicker
{
    RaffleEntry Pick(IReadOnlyList<RaffleEntry> entries);
}

/// <summary>
/// Picks a winner with probability proportional to tickets held: a user with 3 tickets
/// is three times as likely to win as a user with 1.
/// </summary>
public sealed class WeightedWinnerPicker(IRandomSource random) : IWinnerPicker
{
    public RaffleEntry Pick(IReadOnlyList<RaffleEntry> entries)
    {
        var totalTickets = entries.Sum(e => Math.Max(e.TicketCount, 0));
        if (totalTickets == 0)
            throw new BusinessRuleException("There are no paid tickets for this prize.");

        // Lay every ticket on a line and pick one position on it; the owner of that ticket wins.
        // e.g. A has 1 ticket, B has 3: roll 0 -> A, rolls 1..3 -> B.
        var roll = random.NextInt(totalTickets);
        foreach (var entry in entries)
        {
            if (entry.TicketCount <= 0) continue;
            if (roll < entry.TicketCount) return entry;
            roll -= entry.TicketCount;
        }

        throw new InvalidOperationException("Roll fell outside the ticket range.");
    }
}
