using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace DoAnChuyennNganh.Models
{
    public class Sections
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CoursesId { get; set; }

        [Required(ErrorMessage = "Tên phần là bắt buộc")]
        public string Name { get; set; }

        [ForeignKey("CoursesId")]
        public Courses? Courses { get; set; }

        // Danh sách Lectures
        public List<Lectures> Lectures { get; set; } = new List<Lectures>();
    }
}
