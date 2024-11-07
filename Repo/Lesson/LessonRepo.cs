using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;

namespace BrainStormEra.Repo
{
    public class LessonRepo
    {
        private readonly SwpMainContext _context;

        public LessonRepo(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<Lesson> GetLessonByIdAsync(string lessonId)
        {
            if (string.IsNullOrEmpty(lessonId))
            {
                throw new ArgumentException("Lesson ID cannot be null or empty.", nameof(lessonId));
            }

            return await _context.Lessons
                .AsNoTracking() // Optional: improves performance for read-only queries
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);
        }

        public async Task<List<Lesson>> GetLessonsByChapterIdAsync(string chapterId)
        {
            return await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .ToListAsync();
        }
    }
}
