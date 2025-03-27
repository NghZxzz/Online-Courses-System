using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class TeacherApplicationFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } // Tên file

        [Required]
        public string FileUrl { get; set; } // Đường dẫn file (Google Drive)

        public int ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public TeacherApplication Application { get; set; }
    }
}
