using System;
using System.Collections.Generic;

namespace BrainStormEra.Models.ViewModels
{
    public class CourseDetailViewModel
    {
        // Course Information
        public Course Course { get; set; }

        // Rating Information
        public double AverageRating { get; set; }
        public int TotalComments { get; set; }
        public Dictionary<int, double> RatingPercentages { get; set; }

        // Enrollment Status
        public bool IsEnrolled { get; set; }
        public bool IsBanned { get; set; }

        // Course Categories
        public List<string> CourseCategories { get; set; }

        // Learners Count
        public int LearnersCount { get; set; }

        // Feedback Comments
        public List<Feedback> Comments { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // User Role
        public string UserRole { get; set; }

        // Creator's Full Name
        public string CreatedByName { get; set; }

        // Total Lessons
        public int TotalLessons { get; set; }
    }
}
