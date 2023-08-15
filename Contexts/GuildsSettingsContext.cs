using hellgate.Models;
using Microsoft.EntityFrameworkCore;

namespace hellgate.Contexts
{
    public class GuildsSettingsContext : DbContext
    {
        public DbSet<GuildSettings> GuildsSettings { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        public GuildsSettingsContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=GuildSettings.db");
        }
    }
}
