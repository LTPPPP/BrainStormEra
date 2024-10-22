using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Course
{
    public class DeleteCourseViewModel
    {
        public string CourseId { get; set; } = null!;

        public string CourseName { get; set; } = null!;

        public string? CourseDescription { get; set; }

        public int? CourseStatus { get; set; }

        public string? CoursePicture { get; set; }

        public decimal Price { get; set; }

        public DateTime CourseCreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public int? PaymentPoint { get; set; }

        public string? UserPicture { get; set; }

        public virtual Role? UserRoleNavigation { get; set; }

        public byte? StarRating { get; set; }

        public string UserId { get; set; } = null!;

        public int? UserRole { get; set; }

        public string Username { get; set; } = null!;


        public virtual ICollection<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();
    }
}
