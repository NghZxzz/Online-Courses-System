using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class Subject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên chủ đề là bắt buộc")]
        [StringLength(50)]
        public string Name { get; set; }

        public List<Courses>? Courses { get; set; }
    }
}
