using DoAnChuyennNganh.Data;
using DoAnChuyennNganh.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyennNganh.Services.OpenAI
{
    public class CourseService
    {
        private readonly ApplicationDbContext _context;

        public CourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả các khóa học đã được phê duyệt
        public async Task<string> GetCoursesAsTextAsync()
        {
            var courses = await _context.Courses
                                        .Where(c => c.Status == CourseStatus.Approved)
                                        .Include(c => c.Subject)
                                        .ToListAsync();

            if (!courses.Any()) return "Hiện không có khóa học nào được phê duyệt.";

            return string.Join("\n", courses.Select(c =>
                $"- {c.Name}: {c.Description ?? "Không có mô tả"} (Giá: {c.Price} VNĐ)"));
        }
    }


}
