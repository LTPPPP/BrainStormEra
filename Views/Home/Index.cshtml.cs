using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Home
{
    public class HomePageGuestViewtModel
    {

        public List<BrainStormEra.Models.Course> EnrolledCourses { get; set; } = new List<BrainStormEra.Models.Course>();

        // Kh�a h?c ?? xu?t
        public List<ManagementCourseViewModel> RecommendedCourses { get; set; } = new List<ManagementCourseViewModel>();

        // Th�ng tin th�nh t�ch
        public List<BrainStormEra.Models.Achievement> Achievements { get; set; } = new List<BrainStormEra.Models.Achievement>();

        public IEnumerable<BrainStormEra.Models.Notification> Notifications { get; set; }

        public List<BrainStormEra.Models.CourseCategory> Categories { get; set; } = new List<BrainStormEra.Models.CourseCategory>();

    }
}
