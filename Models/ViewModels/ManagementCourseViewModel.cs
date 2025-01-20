namespace BrainStormEra.Models
{
    public class ManagementCourseViewModel
    {
        public String CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseDescription { get; set; }
        public int CourseStatus { get; set; }
        public string CoursePicture { get; set; }
        public decimal Price { get; set; }
        public DateTime CourseCreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public byte? StarRating { get; set; }
        public List<CourseCategory> CourseCategories { get; set; }
    }
}
