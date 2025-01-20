using BrainStormEra.Models;
using BrainStormEra.Repo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Points
{
    public class PointsController : Controller
    {
        private readonly ILogger<PointsController> _logger;
        private readonly SwpMainContext _context;

        public PointsController(ILogger<PointsController> logger, SwpMainContext context)
        {
            _logger = logger;
            _context = context;
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

            var learnersQuery = _context.Accounts.Where(a => a.UserRole == 3 && a.UserId.StartsWith("LN"));

            if (!string.IsNullOrEmpty(search))
            {
                learnersQuery = learnersQuery.Where(a => a.UserId.Contains(search) || a.FullName.Contains(search));
            }

            var learners = await learnersQuery.ToListAsync();
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

            var resultMessage = await UpdatePaymentPoints(request.UserId, request.NewPoints);

            if (resultMessage == "User not found!" || resultMessage == "The points must be between 1,000 and 20,000,000.")
            {
                return Json(new { success = false, message = resultMessage });
            }

            return Json(new { success = true, message = resultMessage });
        }

        private async Task<string> UpdatePaymentPoints(string userId, decimal newPoints)
        {
            if (newPoints < 1000 || newPoints > 20000000)
            {
                return "The points must be between 1,000 and 20,000,000.";
            }

            var user = await _context.Accounts.FindAsync(userId);
            if (user == null)
            {
                return "User not found!";
            }

            user.PaymentPoint += newPoints;
            _context.Accounts.Update(user);
            await _context.SaveChangesAsync();

            var newPaymentId = await GetNewPaymentId();

            var payment = new Payment
            {
                PaymentId = newPaymentId,
                UserId = userId,
                PaymentDescription = $"{userId} - {newPoints:N0} points update",
                Amount = newPoints,
                PointsEarned = (int)newPoints,
                PaymentStatus = "Completed",
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return "Payment points updated and transaction logged successfully!";
        }

        private async Task<string> GetNewPaymentId()
        {
            var maxPaymentId = await _context.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefaultAsync();
            if (maxPaymentId == null)
            {
                return "PA001";
            }

            if (int.TryParse(maxPaymentId.PaymentId.Substring(2), out int idNumber))
            {
                return $"PA{idNumber + 1:D3}";
            }

            return "PA001";
        }

        public class UpdatePointsRequest
        {
            public string UserId { get; set; }
            public decimal NewPoints { get; set; }
        }
    }
}
