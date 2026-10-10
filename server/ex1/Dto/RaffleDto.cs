namespace ex1.DTOs
{
    public class WinnerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class LotteryResultDto
    {
        public int PrizeId { get; set; }
        public int? WinnerUserId { get; set; }
        public WinnerDto? Winner { get; set; }

        /// <summary>True once the winner notification is queued; the email itself is sent by the notification worker.</summary>
        public bool NotificationQueued { get; set; }

        /// <summary>Kept for existing clients; mirrors <see cref="NotificationQueued"/>.</summary>
        public bool EmailSent => NotificationQueued;
    }

    /// <summary>One row of dbo.usp_GetRaffleReport.</summary>
    public class RaffleReportRowDto
    {
        public int PrizeId { get; set; }
        public string PrizeName { get; set; } = string.Empty;
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public bool IsRaffleDone { get; set; }
        public int? WinnerUserId { get; set; }
        public string? WinnerName { get; set; }
    }
}
