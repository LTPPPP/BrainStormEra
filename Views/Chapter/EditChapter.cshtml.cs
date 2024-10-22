using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Chapter
{
    public class EditChapterViewModel
    {

        public string ChapterId { get; set; } = null!;

        public string? CourseId { get; set; }

        public string ChapterName { get; set; } = null!;

        public string? ChapterDescription { get; set; }

        public int? ChapterOrder { get; set; }

        public int? ChapterStatus { get; set; }
    }
}
