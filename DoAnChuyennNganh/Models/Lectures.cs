using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DoAnChuyennNganh.Models
{
    public class Lectures
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SectionId { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Video_url { get; set; }
        public string? Document_url { get; set; }

        public DateOnly CreateDate { get; set; }

        public string? Description { get; set; }


        [ForeignKey("SectionId")]
        public Sections? Sections { get; set; }

        public Quiz? Quiz { get; set; }
    }
}

