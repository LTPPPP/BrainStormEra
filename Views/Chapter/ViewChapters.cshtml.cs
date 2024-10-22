using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrainStormEra.Models;

namespace BrainStormEra.Views.Chapter
{
    public class ViewChaptersViewModel
    {
        public string ChapterId { get; set; } = null!;

        public string? CourseId { get; set; }

        public string ChapterName { get; set; } = null!;

        public string? ChapterDescription { get; set; }

        public int? ChapterOrder { get; set; }

        public int? ChapterStatus { get; set; }

        public DateTime ChapterCreatedAt { get; set; }

    }
}
