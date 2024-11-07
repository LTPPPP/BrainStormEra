using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;

namespace BrainStormEra.Repo.Course
{
    public class CourseRepo
    {
        private readonly SwpMainContext _context;

        public CourseRepo(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<Models.Course> GetCourseByIdAsync(string courseId)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
        }
    }
}
