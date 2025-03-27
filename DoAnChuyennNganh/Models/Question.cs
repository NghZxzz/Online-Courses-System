using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int QuizId { get; set; }

        [Required]
        public string Text { get; set; } // Nội dung câu hỏi

        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; }

        public List<Answer> Answers { get; set; } = new List<Answer>();
    }
}
