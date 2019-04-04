
using SharedArea.Entities;
using Microsoft.EntityFrameworkCore;
using SharedArea.Utils;

namespace StorageService
{
    public class DatabaseContext : SharedArea.DbContexts.DatabaseContext
    {
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageSeen> MessageSeens { get; set; }
        
        public DbSet<File> Files { get; set; }
        public DbSet<FileUsage> FileUsages { get; set; }
        
        public DbSet<Workership> Workerships { get; set; }
        
        public DbSet<BotSecret> BotSecrets { get; set; }
        public DbSet<BotCreation> BotCreations { get; set; }
        public DbSet<BotSubscription> BotSubscriptions { get; set; }
        
        public DbSet<BotStoreHeader> BotStoreHeader { get; set; }
        public DbSet<BotStoreSection> BotStoreSections { get; set; }
        
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleSecret> ModuleSecrets { get; set; }
        public DbSet<ModuleCreation> ModuleCreations { get; set; }
        public DbSet<ModulePermission> ModulePermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                //.UseNpgsql(ConnStringGenerator.GenerateDefaultConnectionString("MessengerPlatformDb"));
                .UseNpgsql(ConnStringGenerator.GenerateSpecificConnectionString("MessengerPlatformDb", "keyhan", "3g5h165tsK65j1s564L69ka5R168kk37sut5ls3Sk2t"));
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Photo>().HasBaseType<File>();
            modelBuilder.Entity<Audio>().HasBaseType<File>();
            modelBuilder.Entity<Video>().HasBaseType<File>();
            modelBuilder.Entity<Document>().HasBaseType<File>();
            
            modelBuilder.Entity<TextMessage>().HasBaseType<Message>();
            modelBuilder.Entity<PhotoMessage>().HasBaseType<Message>();
            modelBuilder.Entity<AudioMessage>().HasBaseType<Message>();
            modelBuilder.Entity<VideoMessage>().HasBaseType<Message>();
            modelBuilder.Entity<ServiceMessage>().HasBaseType<Message>();

            modelBuilder.Entity<FileUsage>()
                .HasIndex(fu => new {fu.FileId, fu.RoomId})
                .IsUnique();
            
            modelBuilder.Entity<MessageSeen>()
                .HasIndex(ms => new {ms.MessageId, ms.UserId})
                .IsUnique();
            
            modelBuilder.Entity<BotCreation>()
                .HasIndex(bc => new {bc.BotId, bc.CreatorId})
                .IsUnique();

            modelBuilder.Entity<BotSubscription>()
                .HasIndex(bs => new {bs.BotId, bs.SubscriberId})
                .IsUnique();

            modelBuilder.Entity<Workership>()
                .HasIndex(ws => new {ws.BotId, ws.RoomId})
                .IsUnique();

            modelBuilder.Entity<Invite>()
                .Property(i => i.InviteId)
                .ValueGeneratedNever();

            modelBuilder.Entity<User>()
                .Property(u => u.BaseUserId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Complex>()
                .Property(u => u.ComplexId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Room>()
                .Property(u => u.RoomId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Membership>()
                .Property(u => u.MembershipId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<UserSecret>()
                .Property(u => u.UserSecretId)
                .ValueGeneratedNever();

            modelBuilder.Entity<ComplexSecret>()
                .Property(u => u.ComplexSecretId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Session>()
                .Property(u => u.SessionId)
                .ValueGeneratedNever();

            modelBuilder.Entity<Bot>()
                .Property(b => b.BaseUserId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<Contact>()
                .Property(b => b.ContactId)
                .ValueGeneratedNever();
            
            modelBuilder.Entity<MemberAccess>()
                .Property(m => m.MemberAccessId)
                .ValueGeneratedNever();
        }
    }
}