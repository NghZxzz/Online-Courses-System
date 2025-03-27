using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int LectureId { get; set; }

        [Required]
        public string Title { get; set; } // Tên bài trắc nghiệm

        [ForeignKey("LectureId")]
        public Lectures Lecture { get; set; }

        public List<Question> Questions { get; set; } = new List<Question>();
    }
}
