using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using BrainStormEra.Models;

namespace BrainStormEra.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly SwpMainContext _context;

        public FeedbackController(SwpMainContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] FeedbackViewModel model)
        {
            string userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to submit feedback." });
            }

            bool isCourseCreator = await _context.Courses
                .AnyAsync(c => c.CreatedBy == userId && c.CourseId == model.CourseId);

            if (!userRole.Equals("1") && !isCourseCreator)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == model.CourseId);

                if (enrollment == null || enrollment.EnrollmentStatus != 1) // Assuming 1 means enrolled
                {
                    return Json(new { success = false, message = "You are not allowed to submit feedback for this course." });
                }
            }

            if (await _context.Feedbacks.AnyAsync(f => f.CourseId == model.CourseId && f.UserId == userId && (f.HiddenStatus == false || f.HiddenStatus == null)))
            {
                return Json(new { success = false, message = "You have already submitted feedback for this course." });
            }

            if (string.IsNullOrEmpty(model.CourseId) || string.IsNullOrEmpty(model.Comment) || model.StarRating < 1 || model.StarRating > 5)
            {
                return Json(new { success = false, message = "Invalid input data." });
            }

            var feedback = new Feedback
            {
                FeedbackId = await GenerateFeedbackIdAsync(),
                CourseId = model.CourseId,
                UserId = userId,
                StarRating = (byte)model.StarRating,
                Comment = model.Comment,
                FeedbackDate = DateOnly.FromDateTime(DateTime.UtcNow),
                FeedbackCreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thank you for your feedback!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFeedback([FromBody] string feedbackId)
        {
            string userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            var feedback = await _context.Feedbacks.FindAsync(feedbackId);

            if (feedback == null)
            {
                return Json(new { success = false, message = "Feedback not found." });
            }

            if (userRole == "3")
            {
                feedback.HiddenStatus = true;
            }
            else
            {
                _context.Feedbacks.Remove(feedback);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Comment deleted successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> EditFeedback([FromBody] FeedbackViewModel model)
        {
            string userId = Request.Cookies["user_id"];

            var feedback = await _context.Feedbacks.FindAsync(model.FeedbackId);

            if (feedback == null || feedback.UserId != userId)
            {
                return Json(new { success = false, message = "You cannot edit this comment" });
            }

            feedback.Comment = model.Comment;
            feedback.StarRating = (byte)model.StarRating;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Comment updated successfully" });
        }

        private async Task<string> GenerateFeedbackIdAsync()
        {
            string newId = "FE001";
            var lastFeedback = await _context.Feedbacks.OrderByDescending(f => f.FeedbackId).FirstOrDefaultAsync();

            if (lastFeedback != null)
            {
                int currentIdNumber = int.Parse(lastFeedback.FeedbackId.Substring(2));
                newId = "FE" + (currentIdNumber + 1).ToString("D3");
            }

            return newId;
        }

        public class FeedbackViewModel
        {
            public string FeedbackId { get; set; }
            public string CourseId { get; set; }
            public int StarRating { get; set; }
            public string Comment { get; set; }
        }
    }
}
