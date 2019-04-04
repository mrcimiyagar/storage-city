using Microsoft.EntityFrameworkCore;
using SharedArea.Entities;

namespace SharedArea.DbContexts
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSecret> UserSecrets { get; set; }
        public DbSet<Bot> Bots { get; set; }
        public DbSet<BotSecret> BotSecrets { get; set; }
        public DbSet<BotCreation> BotCreations { get; set; }
        public DbSet<Storage> Storages { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSession>().HasBaseType<Session>();
            modelBuilder.Entity<BotSession>().HasBaseType<Session>();
            
            modelBuilder.Entity<StorageAgentUser>().HasBaseType<StorageAgent>();
            modelBuilder.Entity<StorageAgentBot>().HasBaseType<StorageAgent>();
        }
    }
}