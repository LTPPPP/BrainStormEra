using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BrainStormEra.Views.Course
{
    public class CreateCourseViewModel
    {

        [DisplayName("ID")]
        [Required(ErrorMessage = "Course ID is required")]
        public string CourseId { get; set; } = null!;


        [DisplayName("CourseName")]
        [Required(ErrorMessage = "Course Name is required")]
        [StringLength(100, ErrorMessage = "Course Name cannot exceed 100 characters")]
        public string CourseName { get; set; } = null!;


        [Required(ErrorMessage = "Description is required")]
        [DisplayName("CourseDescription")]
        [StringLength(1000, ErrorMessage = "Course Description cannot exceed 500 characters")]
        public string? CourseDescription { get; set; }


        [Required(ErrorMessage = "Picture is required")]
        [DisplayName("CoursePicture")]
        public IFormFile? CoursePicture { get; set; }


        [DisplayName("Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]
        public decimal Price { get; set; }

		public List<string> CategoryIds { get; set; } = new List<string>(); // Ch?a nhi?u category ID

		public List<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();



    }


}
