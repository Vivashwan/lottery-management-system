namespace Lottery.Messaging.Email
{
    public interface IEmailService
    {
        Task SendWinnerEmail(string toEmail, string userName, string prizeName);
    }
}