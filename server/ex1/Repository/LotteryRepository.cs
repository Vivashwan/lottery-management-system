using ex1.Data;
using ex1.DTOs;
using ex1.Interfaces.Repositorys;
using ex1.Lottery;
using ex1.Models;
using Microsoft.EntityFrameworkCore;

namespace ex1.Repository
{
    public class LotteryRepository : ILotteryRepository
    {
        private readonly StoreContext _context;

        public LotteryRepository(StoreContext context)
        {
            _context = context;
        }

        public async Task<List<RaffleEntry>> GetRaffleEntries(int prizeId)
        {
            // One row per user, weighted by how many paid tickets they bought for this prize
            return await _context.CartItems
                .Where(ci => ci.PrizeId == prizeId && ci.Cart.Status == CartStatus.Paid)
                .GroupBy(ci => new
                {
                    ci.Cart.UserId,
                    ci.Cart.User.FirstName,
                    ci.Cart.User.LastName,
                    ci.Cart.User.Email,
                })
                .OrderBy(g => g.Key.UserId)
                .Select(g => new RaffleEntry(
                    g.Key.UserId, g.Key.FirstName, g.Key.LastName, g.Key.Email, g.Sum(ci => ci.Quantity)))
                .ToListAsync();
        }

        public async Task<Prize?> GetPrizeById(int prizeId)
        {
            return await _context.Prizes.FirstOrDefaultAsync(p => p.Id == prizeId);
        }

        public async Task<bool> TryRecordWinner(int prizeId, int userId)
        {
            // dbo.usp_RecordWinner updates the row only if IsRaffleDone = 0, in a single statement,
            // so two concurrent draws can never both succeed.
            var affected = await _context.Database
                .SqlQuery<int>($"EXEC dbo.usp_RecordWinner @PrizeId = {prizeId}, @WinnerUserId = {userId}")
                .ToListAsync();

            return affected.Single() == 1;
        }

        public async Task<List<RaffleReportRowDto>> GetRaffleReport()
        {
            return await _context.Database
                .SqlQuery<RaffleReportRowDto>($"EXEC dbo.usp_GetRaffleReport")
                .ToListAsync();
        }
    }
}
