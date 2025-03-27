using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DoAnChuyennNganh.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public DateTime? Birthday { get; set; }


        public string? Gender { get; set; }

        [Required]
        public string Status { get; set; } = "Đang hoạt động";

        public string? HinhAnh { get; set; }

        public int? Money { get; set; } = 0;
    }
}
