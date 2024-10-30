using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrainStormEra.Models;
using BrainStormEra.Views.Course;

namespace BrainStormEra.Views.Home
{
    public class HomePageLearnerViewModel
    {

        public string? FullName { get; set; }
        public decimal? PaymentPoint { get; set; }
        public string? UserPicture { get; set; }


        // Th�ng tin kh�a h?c ?� ??ng k�
        public int CompletedCoursesCount { get; set; }
        public int TotalCoursesEnrolled { get; set; }
        public int Ranking { get; set; }
        public byte? StarRating { get; set; }




        public virtual ICollection<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();
        public List<BrainStormEra.Models.Course> EnrolledCourses { get; set; } = new List<BrainStormEra.Models.Course>();

        // Kh�a h?c ?? xu?t
        public List<ManagementCourseViewModel> RecommendedCourses { get; set; } = new List<ManagementCourseViewModel>();

        // Th�ng tin th�nh t�ch
        public List<BrainStormEra.Models.Achievement> Achievements { get; set; } = new List<BrainStormEra.Models.Achievement>();

        public IEnumerable<BrainStormEra.Models.Notification> Notifications { get; set; }




    }
}
