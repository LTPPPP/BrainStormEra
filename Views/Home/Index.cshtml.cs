using System;
using System.Collections.Generic;

namespace BrainStormEra.Views.Home
{
    public class HomePageGuestViewtModel
    {
        // Nested class to represent individual courses with specific display properties
        public class ManagementCourseViewModel
        {
            public string CourseId { get; set; }
            public string CourseName { get; set; }
            public string CourseDescription { get; set; }
            public int CourseStatus { get; set; }
            public string CoursePicture { get; set; }
            public decimal Price { get; set; }
            public DateTime CourseCreatedAt { get; set; }
            public string CreatedBy { get; set; }  // Name of the course creator
            public byte? StarRating { get; set; }  // Average star rating

            // Optional list of course categories
            public List<string> CourseCategories { get; set; } = new List<string>();
        }

        // List to store courses that the guest user might enroll in
        public List<BrainStormEra.Models.Course> EnrolledCourses { get; set; } = new List<BrainStormEra.Models.Course>();

        // List to store recommended courses to display on the homepage
        public List<ManagementCourseViewModel> RecommendedCourses { get; set; } = new List<ManagementCourseViewModel>();

        // List to store achievements associated with the user or courses
        public List<BrainStormEra.Models.Achievement> Achievements { get; set; } = new List<BrainStormEra.Models.Achievement>();

        // List to store notifications for the user
        public IEnumerable<BrainStormEra.Models.Notification> Notifications { get; set; }

        // List to store course categories available on the platform
        public List<BrainStormEra.Models.CourseCategory> Categories { get; set; } = new List<BrainStormEra.Models.CourseCategory>();
    }
}
