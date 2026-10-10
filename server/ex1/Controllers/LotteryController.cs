using ex1.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ex1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "manager")]
    public class LotteryController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;

        public LotteryController(ILotteryService lotteryService)
        {
            _lotteryService = lotteryService;
        }

        // Errors (404 / 409 / 400) are mapped by ExceptionHandlingMiddleware from typed exceptions
        [HttpPost("raffle/{prizeId}")]
        public async Task<IActionResult> RafflePrize(int prizeId)
        {
            return Ok(await _lotteryService.RafflePrizeAsync(prizeId));
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport()
        {
            return Ok(await _lotteryService.GetRaffleReportAsync());
        }
    }
}
