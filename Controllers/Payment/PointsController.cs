using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Points
{
    public class PointsController : Controller
    {
        private readonly SwpMainContext _context;

        public PointsController(SwpMainContext context)
        {
            _context = context;
        }

        // Display Update Management page for learner points
        [HttpGet]
        public async Task<IActionResult> UpdateManagement(string search)
        {
            // Retrieve userId and userRole from cookies
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            // Check if the user is an admin
            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized(); // Unauthorized if not an admin
            }

            var learners = _context.Accounts
                .Where(a => a.UserRole == 3 && a.UserId.StartsWith("LN")) // Fetch learners with 'LN' prefix
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                learners = learners.Where(a => a.UserId.Contains(search) || a.FullName.Contains(search));
            }

            var learnerList = await learners.ToListAsync();

            // Explicitly specify the view path to Admin/PointsManagement
            return View("~/Views/Admin/PointsManagement.cshtml", learnerList);
        }

        // Update learner payment points
        [HttpPost]
        public async Task<IActionResult> UpdatePaymentPoints([FromBody] UpdatePointsRequest request)
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized();
            }

            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == request.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found!" });
            }

            user.PaymentPoint = request.NewPoints;
            _context.Accounts.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment points updated successfully!" });
        }
    }

    public class UpdatePointsRequest
    {
        public string UserId { get; set; }
        public decimal NewPoints { get; set; }
    }
}
