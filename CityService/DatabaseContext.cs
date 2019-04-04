
using SharedArea.Entities;
using Microsoft.EntityFrameworkCore;
using SharedArea.Utils;

namespace CityService
{
    public class DatabaseContext : SharedArea.DbContexts.DatabaseContext
    {
        public DbSet<Pending> Pendings { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseNpgsql(ConnStringGenerator.GenerateDefaultConnectionString("CityPlatformDb"));
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}