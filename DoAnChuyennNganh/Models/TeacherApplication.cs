using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public enum ApplicationStatus
    {
        Pending, // Đang chờ duyệt
        Approved, // Đã được duyệt
        Rejected // Bị từ chối
    }
    public class TeacherApplication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // User đăng ký

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; } // Liên kết đến bảng User

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } // Họ tên

        [Required]
        [EmailAddress]
        public string Email { get; set; } // Email

        [Phone]
        public string PhoneNumber { get; set; } // Số điện thoại

        [Required]
        public string Address { get; set; } // Địa chỉ

        public string Degree { get; set; } // Bằng cấp

        [Required]
        public string Major { get; set; } // Chuyên ngành

        public string TeachingExperience { get; set; } // Kinh nghiệm giảng dạy

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public ICollection<TeacherApplicationFile> ApplicationFiles { get; set; } = new List<TeacherApplicationFile>();
    }
}
