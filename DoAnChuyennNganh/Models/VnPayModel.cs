using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class VnPayModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string OrderDescription { get; set; }
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentId { get; set; }

        public Double? Price { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UserId { get; set; } // Thêm trường UserId

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public int CourseId { get; set; } // Liên kết với khóa học

        [ForeignKey("CourseId")]
        public Courses Course { get; set; }
    }
}
