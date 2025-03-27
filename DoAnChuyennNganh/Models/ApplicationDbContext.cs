using DoAnChuyennNganh.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyennNganh.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Sections> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Courses> Courses { get; set; }
        public DbSet<Lectures> Lectures { get; set; }

        public DbSet<CompletedLecture> CompletedLectures { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<VnPayModel> vnPays { get; set; }
        public DbSet<TeacherApplication> TeacherApplications { get; set; }
        public DbSet<TeacherApplicationFile> TeacherApplicationFiles { get; set; }

        public DbSet<Review> Reviews { get; set; }
    }
}