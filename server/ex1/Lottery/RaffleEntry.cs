namespace ex1.Lottery;

/// <summary>One participant in a prize draw, with the number of paid tickets they hold.</summary>
public sealed record RaffleEntry(int UserId, string FirstName, string LastName, string Email, int TicketCount)
{
    public string FullName => $"{FirstName} {LastName}";
}
