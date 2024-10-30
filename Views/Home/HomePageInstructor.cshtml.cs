using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Home
{
    public class HomePageInstructorViewModel
    {

        public string? FullName { get; set; }
        public decimal? PaymentPoint { get; set; }
        public string? UserPicture { get; set; }


        // Th�ng tin kh�a h?c ?� ??ng k�
        public int CompletedCoursesCount { get; set; }
        public int TotalCoursesEnrolled { get; set; }
        public int Ranking { get; set; }
        public byte? StarRating { get; set; }


        public List<BrainStormEra.Models.Course> EnrolledCourses { get; set; } = new List<BrainStormEra.Models.Course>();

        // Kh�a h?c ?? xu?t
        public List<ManagementCourseViewModel> RecommendedCourses { get; set; } = new List<ManagementCourseViewModel>();

        // Th�ng tin th�nh t�ch
        public List<BrainStormEra.Models.Achievement> Achievements { get; set; } = new List<BrainStormEra.Models.Achievement>();

        public IEnumerable<BrainStormEra.Models.Notification> Notifications { get; set; }

        public List<BrainStormEra.Models.CourseCategory> Categories { get; set; } = new List<BrainStormEra.Models.CourseCategory>();

    }
}
