using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Ranking
{
    public class RankingController : Controller
    {
        private readonly SwpMainContext _context;

        public RankingController(SwpMainContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Ranking()
        {
            var rankings = await _context.Accounts
                .Where(a => a.UserRole == 3) // Only get learners
                .Select(a => new
                {
                    a.UserId,
                    a.Username,
                    a.FullName,
                    CompletedCourses = _context.LessonCompletions
                        .Where(lc => lc.UserId == a.UserId)
                        .Select(lc => lc.Lesson.Chapter.CourseId)
                        .Distinct()
                        .Count(),
                    a.UserPicture
                })
                .OrderByDescending(a => a.CompletedCourses)
                .ToListAsync();

            return View("~/Views/Admin/Ranking.cshtml", rankings);
        }
    }
}
