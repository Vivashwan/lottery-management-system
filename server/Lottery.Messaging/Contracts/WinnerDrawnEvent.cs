namespace Lottery.Messaging.Contracts;

/// <summary>Published by the API after a raffle winner is committed; consumed by the notification worker.</summary>
public sealed record WinnerDrawnEvent(
    int PrizeId,
    string PrizeName,
    int WinnerUserId,
    string WinnerName,
    string WinnerEmail,
    DateTime DrawnAtUtc);
