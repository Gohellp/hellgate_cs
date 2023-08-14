using System.ComponentModel.DataAnnotations;

namespace hellgate.Models
{
    public class GuildSettings
    {
        [Key]
        public required string ServerId { get; set; }

        public string TextCommandPrefix { get; set; } = "+";

        public int PlayerVolume { get; set; } = 85;

        public string? BotChannelId { get; set; } = String.Empty;

        public bool OnlyInBotChannel { get; set; } = false;

        public string? DJRoleId { get; set; } = String.Empty;

        public bool DefaultLoopQueue { get; set; } = false;

        public string AdminRolesIds { get; set; } = String.Empty;

        public ICollection<UserSetting>? Users { get; set; } = new List<UserSetting>();
    }
}
