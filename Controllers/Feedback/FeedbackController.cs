﻿using BrainStormEra.Repo;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly FeedbackRepo _feedbackRepo;

        public FeedbackController(FeedbackRepo feedbackRepo)
        {
            _feedbackRepo = feedbackRepo;
        }


        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] FeedbackViewModel model)
        {
            string userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to submit feedback." });
            }

            // Kiểm tra nếu người dùng đã gửi feedback cho khóa học này
            if (await _feedbackRepo.HasUserFeedbackAsync(model.CourseId, userId))
            {
                return Json(new { success = false, message = "You have already submitted feedback for this course." });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(model.CourseId) || string.IsNullOrEmpty(model.Comment) || model.StarRating < 1 || model.StarRating > 5)
            {
                return Json(new { success = false, message = "Invalid input data." });
            }

            // Tạo phản hồi trong cơ sở dữ liệu
            await _feedbackRepo.CreateFeedbackAsync(model.CourseId, userId, model.StarRating, model.Comment);

            return Json(new { success = true, message = "Thank you for your feedback!" });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteFeedback([FromBody] string feedbackId)
        {
            string userId = Request.Cookies["user_id"];
            if (await _feedbackRepo.CanDeleteFeedbackAsync(feedbackId, userId))
            {
                await _feedbackRepo.DeleteFeedbackAsync(feedbackId);
                return Json(new { success = true, message = "Comment deleted successfully" });
            }
            return Json(new { success = false, message = "You cannot delete this comment" });
        }

        [HttpPost]
        public async Task<IActionResult> EditFeedback([FromBody] FeedbackViewModel model)
        {
            string userId = Request.Cookies["user_id"];
            if (await _feedbackRepo.CanEditFeedbackAsync(model.FeedbackId, userId))
            {
                await _feedbackRepo.UpdateFeedbackAsync(model.FeedbackId, model.Comment, model.StarRating);
                return Json(new { success = true, message = "Comment updated successfully" });
            }
            return Json(new { success = false, message = "You cannot edit this comment" });
        }










        public class FeedbackViewModel
        {
            public string FeedbackId { get; set; } // Thêm thuộc tính này
            public string CourseId { get; set; }
            public int StarRating { get; set; }
            public string Comment { get; set; }
            // Các thuộc tính khác nếu cần
        }


    }
}