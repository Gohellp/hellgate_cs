using Microsoft.EntityFrameworkCore;
using hellgate.Models;

namespace hellgate.Contexts
{
    internal class PQRPContext : DbContext
    {

        public PQRPContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=ProjectQuarantine.db");
        }
    }
}
