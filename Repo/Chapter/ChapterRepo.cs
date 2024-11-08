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
                .OrderBy(ch => ch.ChapterOrder)
                .ToListAsync();
        }

        public async Task<Models.Chapter> GetChapterByIdAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("Chapter ID cannot be null or empty.", nameof(chapterId));
            }

            return await _context.Chapters
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ChapterId == chapterId);
        }

        public async Task<bool> IsChapterNameDuplicateAsync(string chapterName, string chapterId)
        {
            return await _context.Chapters
                .AnyAsync(c => c.ChapterName == chapterName && c.ChapterId != chapterId);
        }

        public async Task UpdateChapterAsync(Models.Chapter chapter)
        {
            _context.Chapters.Update(chapter);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Models.Chapter>> GetAllChaptersByCourseIdAsync(string courseId)
        {
            return await _context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .OrderBy(ch => ch.ChapterOrder)
                .ToListAsync();
        }

        public async Task<Models.Chapter> GetLastChapterInCourseAsync(string courseId)
        {
            return await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .OrderByDescending(c => c.ChapterOrder)
                .FirstOrDefaultAsync();
        }

        public async Task<string> GenerateNewChapterIdAsync()
        {
            var lastChapter = await _context.Chapters
                .OrderByDescending(c => c.ChapterId)
                .FirstOrDefaultAsync();
            return lastChapter == null ? "CH001" : "CH" + (int.Parse(lastChapter.ChapterId.Substring(2)) + 1).ToString("D3");
        }

        public async Task AddChapterAsync(Models.Chapter chapter)
        {
            await _context.Chapters.AddAsync(chapter);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteChaptersAsync(List<string> chapterIds)
        {
            var chaptersToDelete = await _context.Chapters
                .Where(ch => chapterIds.Contains(ch.ChapterId))
                .ToListAsync();

            _context.Chapters.RemoveRange(chaptersToDelete);
            await _context.SaveChangesAsync();
        }
    }
}
