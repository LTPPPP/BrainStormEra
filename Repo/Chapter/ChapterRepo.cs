using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using OpenQA.Selenium;

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
        
        public async Task<Models.Chapter> GetChapterByIdAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("Lesson ID cannot be null or empty.", nameof(chapterId));
            }

            return await _context.Chapters
                .AsNoTracking() // Optional: improves performance for read-only queries
                .FirstOrDefaultAsync(l => l.ChapterId == chapterId);
        }
    }
}
