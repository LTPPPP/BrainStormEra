using BrainStormEra.Controllers.Course;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Course;
using BrainStormEra.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Points
{
    public class PointsController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<CourseController> _logger;
        private readonly PointsRepo _pointRepo;
        public PointsController(IConfiguration configuration, ILogger<CourseController> logger, PointsRepo pointsRepo)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
            _logger = logger;
            _pointRepo = pointsRepo;
        }

        [HttpGet]
        public async Task<IActionResult> UpdateManagement(string search, int pageIndex = 1, int pageSize = 50)
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized();
            }

            var learners = await _pointRepo.GetLearners(search);
            int totalLearners = learners.Count();
            int totalPages = (int)Math.Ceiling(totalLearners / (double)pageSize);

            // Calculate the total points of all learners
            decimal totalPoints = learners.Sum(learner => learner.PaymentPoint.GetValueOrDefault());

            var pagedLearners = learners.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageIndex;
            ViewBag.TotalPoints = totalPoints; // Pass the total points to the view

            return View("~/Views/Admin/PointsManagement.cshtml", pagedLearners);
        }



        [HttpPost]
        public async Task<IActionResult> UpdatePaymentPoints([FromBody] UpdatePointsRequest request)
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized();
            }

            var resultMessage = await _pointRepo.UpdatePaymentPoints(request.UserId, request.NewPoints);

            if (resultMessage == "User not found!" || resultMessage == "The points must be between 1,000 and 20,000,000.")
            {
                return Json(new { success = false, message = resultMessage });
            }

            return Json(new { success = true, message = resultMessage });
        }

        public class UpdatePointsRequest
        {
            public string UserId { get; set; }
            public decimal NewPoints { get; set; }
        }
    }
}
