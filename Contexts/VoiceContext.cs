using Microsoft.EntityFrameworkCore;
using hellgate.Models;

namespace hellgate.Contexts
{
    public class VoiceContext : DbContext
    {

        public DbSet<Voice> Voices { get; set; }
        
        public DbSet<VoiceLog> VoiceLogs { get; set; }

        public VoiceContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=Voices.db");
        }
    }
}
