using ex1.DTOs;
using ex1.Exceptions;
using ex1.Interfaces.Repositorys;
using ex1.Interfaces.Services;
using ex1.Lottery;
using Lottery.Messaging;
using Lottery.Messaging.Contracts;

namespace ex1.services
{
    public class LotteryService : ILotteryService
    {
        private readonly ILotteryRepository _lotteryRepository;
        private readonly IWinnerPicker _winnerPicker;
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<LotteryService> _logger;

        public LotteryService(
            ILotteryRepository lotteryRepository,
            IWinnerPicker winnerPicker,
            IMessagePublisher publisher,
            ILogger<LotteryService> logger)
        {
            _lotteryRepository = lotteryRepository;
            _winnerPicker = winnerPicker;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<LotteryResultDto> RafflePrizeAsync(int prizeId)
        {
            var prize = await _lotteryRepository.GetPrizeById(prizeId)
                ?? throw new NotFoundException($"Prize {prizeId} was not found.");

            if (prize.IsRaffleDone)
                throw new ConflictException($"Prize {prizeId} has already been drawn.");

            var entries = await _lotteryRepository.GetRaffleEntries(prizeId);
            if (entries.Count == 0)
                throw new BusinessRuleException($"Prize {prizeId} has no paid tickets to draw from.");

            var winner = _winnerPicker.Pick(entries);

            // Another request may have drawn this prize between the check above and now;
            // the stored procedure only lets the first one through.
            if (!await _lotteryRepository.TryRecordWinner(prizeId, winner.UserId))
                throw new ConflictException($"Prize {prizeId} was drawn by another request.");

            _logger.LogInformation(
                "Prize {PrizeId} drawn: user {UserId} won with {Tickets} of {TotalTickets} tickets",
                prizeId, winner.UserId, winner.TicketCount, entries.Sum(e => e.TicketCount));

            var result = new LotteryResultDto
            {
                PrizeId = prizeId,
                WinnerUserId = winner.UserId,
                NotificationQueued = await TryQueueWinnerNotification(prize.Name, winner, prizeId),
                Winner = new WinnerDto
                {
                    Id = winner.UserId,
                    FullName = winner.FullName,
                    Email = winner.Email,
                },
            };

            await AppendLotteryResult(result);

            return result;
        }

        public Task<List<RaffleReportRowDto>> GetRaffleReportAsync() => _lotteryRepository.GetRaffleReport();

        private async Task<bool> TryQueueWinnerNotification(string prizeName, RaffleEntry winner, int prizeId)
        {
            // The winner is already committed, so a broker outage must not fail the draw.
            try
            {
                await _publisher.PublishAsync(new WinnerDrawnEvent(
                    prizeId, prizeName, winner.UserId, winner.FullName, winner.Email, DateTime.UtcNow));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not queue winner notification for prize {PrizeId}", prizeId);
                return false;
            }
        }

        private async Task AppendLotteryResult(LotteryResultDto result)
        {
            try
            {
                Directory.CreateDirectory("Reports");
                var path = "Reports/LotterySummary.csv";
                var exists = File.Exists(path);

                using var writer = new StreamWriter(path, true);
                if (!exists)
                    await writer.WriteLineAsync("PrizeId,WinnerUserId");

                await writer.WriteLineAsync($"{result.PrizeId},{result.WinnerUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing lottery summary file");
            }
        }
    }
}
