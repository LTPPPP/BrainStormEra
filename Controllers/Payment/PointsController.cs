using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized();
            }

            var learners = _context.Accounts
                .Where(a => a.UserRole == 3 && a.UserId.StartsWith("LN"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                learners = learners.Where(a => a.UserId.Contains(search) || a.FullName.Contains(search));
            }

            var learnerList = await learners.ToListAsync();

            return View("~/Views/Admin/PointsManagement.cshtml", learnerList);
        }

        // Update learner payment points by adding to the existing points and logging the transaction
        [HttpPost]
        public async Task<IActionResult> UpdatePaymentPoints([FromBody] UpdatePointsRequest request)
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            // Authorization check for admin role
            if (string.IsNullOrEmpty(userId) || userRole != "1")
            {
                return Unauthorized();
            }

            // Validate points range
            if (request.NewPoints < 1000 || request.NewPoints > 20000000)
            {
                return Json(new { success = false, message = "The points must be between 1,000 and 20,000,000." });
            }

            // Retrieve the user account
            var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == request.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found!" });
            }

            // Update the user's points by adding the new points to the existing points
            user.PaymentPoint = (user.PaymentPoint ?? 0) + request.NewPoints;
            _context.Accounts.Update(user);

            // Generate new Payment ID by getting the max ID and incrementing
            var maxPaymentId = await _context.Payments
                .OrderByDescending(p => p.PaymentId)
                .Select(p => p.PaymentId)
                .FirstOrDefaultAsync();

            int newPaymentIdNumber = 1;
            if (!string.IsNullOrEmpty(maxPaymentId) && int.TryParse(maxPaymentId.Substring(2), out var idNumber))
            {
                newPaymentIdNumber = idNumber + 1;
            }

            var newPaymentId = $"PA{newPaymentIdNumber:D3}";

            // Create a new payment record
            var newPayment = new Payment
            {
                PaymentId = newPaymentId,
                UserId = request.UserId,
                PaymentDescription = $"{request.UserId} - {request.NewPoints.ToString("N0")} points update",
                Amount = request.NewPoints,
                PointsEarned = (int)request.NewPoints,
                PaymentStatus = "Completed",
                PaymentDate = DateTime.Now
            };


            // Add the new payment record to the database
            _context.Payments.Add(newPayment);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment points updated and transaction logged successfully!" });
        }
    }

    public class UpdatePointsRequest
    {
        public string UserId { get; set; }
        public decimal NewPoints { get; set; }
    }
}
