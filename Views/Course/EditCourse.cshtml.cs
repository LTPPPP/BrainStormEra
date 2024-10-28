using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BrainStormEra.Views.Course
{
    public class EditCourseViewModel
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


        [DisplayName("CoursePicture")]
        public IFormFile? CoursePicture { get; set; }

        public string? CoursePictureFile { get; set; }  // Dùng để upload file


        [DisplayName("Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]
        public decimal Price { get; set; }

        public List<string> CategoryIds { get; set; } = new List<string>(); // Chứa ID các category đã chọn
        public List<CourseCategory> SelectedCategories { get; set; } = new List<CourseCategory>(); // Danh sách các category đã chọn

        public List<int> ExistingCategories { get; set; } // Danh sách các ID category đã có từ trước
        public List<CourseCategory> CourseCategories { get; set; } = new List<CourseCategory>();

    }
}
