using BrainStormEra.Models;

public class AddLessonViewModel
{
    public Lesson Lesson { get; set; }
    public IFormFile LessonContentFile { get; set; }
    public string LessonLink { get; set; }
}