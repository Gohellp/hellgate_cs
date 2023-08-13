using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hellgate.Models
{
    public class Voice
    {
        [Key]
        [Required]
        [StringLength(25)]
        public required string voiceId { get; set; }

        [Required]
        [StringLength(25)]
        public required string OwnerId { get; set; }
    }
}
