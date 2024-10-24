using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BrainStormEra.Controllers.Account
{
    public class InstructorController : Controller
    {
        private readonly SwpMainFpContext _context;

        public InstructorController(SwpMainFpContext context)
        {
            _context = context;
        }

        public IActionResult ViewListOfInstructorCourse()
        {
            var userId = Request.Cookies["userId"];
            if (userId.IsNullOrEmpty())
            {
                return Unauthorized();
            }

            var courses = _context.Courses
                          .Where(c => _context.Enrollments
                              .Any(e => e.UserId == userId && e.CourseId == c.CourseId))
                          .ToList();

            return View(courses);
        }

    }
}
