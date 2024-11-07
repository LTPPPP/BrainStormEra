using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;

namespace BrainStormEra.Repo.Chapter
{
    public class ChapterRepo
    {
        private readonly SwpMainContext _context;

        public ChapterRepo(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<List<Models.Chapter>> GetChaptersByCourseIdAsync(string courseId)
        {
            return await _context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .ToListAsync();
        }
    }
}
