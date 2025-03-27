using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace DoAnChuyennNganh.Models
{
    public enum CourseStatus
    {
        Pending,       // Chưa xét duyệt
        UnderReview,   // Đang xét duyệt
        Approved,      // Đã xét duyệt
        Rejected,      // Đã từ chối
        Hiden          // Đã bị ẩn
    }
    public class Courses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int SubjectId { get; set; }
        [Required(ErrorMessage = "Tên khóa học là bắt buộc")]
        [StringLength(50)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Image_url { get; set; }
        [Required]
        public int Price { get; set; }
        public CourseStatus Status { get; set; } = CourseStatus.Pending;
        public DateTime? SubmittedDate { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        public List<Sections> Sections { get; set; } = new List<Sections>();
    }
}
