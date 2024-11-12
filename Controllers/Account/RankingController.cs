using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Repo;
namespace BrainStormEra.Controllers.Ranking
{
    public class RankingController : Controller
    {
        private readonly AccountRepo _accountRepo;

        public RankingController(AccountRepo accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public async Task<IActionResult> Ranking()
        {
            var rankings = await _accountRepo.GetUserRankingAsync();
            return View("~/Views/Admin/Ranking.cshtml", rankings);
        }
    }
}