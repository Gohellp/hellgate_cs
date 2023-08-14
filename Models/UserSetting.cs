using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hellgate.Models
{
    public class UserSetting
    {
        [Key]
        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string GuildId { get; set; }

        public bool AllowUseCommands { get; set; } = true;


        public required GuildSettings Guild { get; set; }
    }
}
