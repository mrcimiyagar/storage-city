
using SharedArea.Entities;
using Microsoft.EntityFrameworkCore;
using SharedArea.Utils;

namespace StorageService
{
    public class DatabaseContext : SharedArea.DbContexts.DatabaseContext
    {
        public DbSet<File> Files { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseNpgsql(ConnStringGenerator.GenerateDefaultConnectionString("StorageServiceDb"));
                //.UseNpgsql(ConnStringGenerator.GenerateSpecificConnectionString("MessengerPlatformDb", "keyhan", "3g5h165tsK65j1s564L69ka5R168kk37sut5ls3Sk2t"));
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<BotCreation>()
                .HasIndex(bc => new {bc.BotId, bc.CreatorId})
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .ValueGeneratedNever();

            modelBuilder.Entity<UserSecret>()
                .Property(u => u.UserSecretId)
                .ValueGeneratedNever();

            modelBuilder.Entity<Bot>()
                .Property(b => b.BotId)
                .ValueGeneratedNever();

            modelBuilder.Entity<BotSecret>()
                .Property(bs => bs.BotSecretId)
                .ValueGeneratedNever();

            modelBuilder.Entity<BotCreation>()
                .Property(bc => bc.BotCreationId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Session>()
                .Property(u => u.SessionId)
                .ValueGeneratedNever();
        }
    }
}