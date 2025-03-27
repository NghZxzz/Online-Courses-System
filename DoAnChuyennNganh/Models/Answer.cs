using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class Answer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int QuestionId { get; set; }

        [Required]
        public string Text { get; set; } // Nội dung đáp án

        public bool IsCorrect { get; set; } // Đánh dấu đáp án đúng

        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
    }
}
