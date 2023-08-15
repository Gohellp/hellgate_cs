using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hellgate.Models
{
    public class UserSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Index(IsUnique = false)]
        public required string UserId { get; set; }

        public string? GuildId { get; set; }

        public bool AllowUseCommands { get; set; } = true;


        public GuildSettings? Guild { get; set; }
    }
}
