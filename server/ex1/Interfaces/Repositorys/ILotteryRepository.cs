using ex1.DTOs;
using ex1.Lottery;
using ex1.Models;

namespace ex1.Interfaces.Repositorys
{
    public interface ILotteryRepository
    {
        /// <summary>Participants with at least one paid ticket for the prize, with their ticket counts.</summary>
        Task<List<RaffleEntry>> GetRaffleEntries(int prizeId);

        Task<Prize?> GetPrizeById(int prizeId);

        /// <summary>
        /// Atomically records the winner. Returns false if the prize was already drawn,
        /// including by a concurrent request that committed first.
        /// </summary>
        Task<bool> TryRecordWinner(int prizeId, int userId);

        Task<List<RaffleReportRowDto>> GetRaffleReport();
    }
}
