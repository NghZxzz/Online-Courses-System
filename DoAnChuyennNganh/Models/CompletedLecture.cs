using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class CompletedLecture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public DateTime CompletedAt { get; set; }

        public ApplicationUser User { get; set; }
        public Lectures Lecture { get; set; }
    }
}
