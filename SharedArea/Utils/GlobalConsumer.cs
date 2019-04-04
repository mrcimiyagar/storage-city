using System;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.DbContexts;
using SharedArea.Entities;

namespace SharedArea.Utils
{
    public class GlobalConsumer<T> : IConsumer<SessionCreatedNotif>, IConsumer<UserProfileUpdatedNotif>
        , IConsumer<SessionUpdatedNotif>, IConsumer<ConsolidateLogoutRequest>, IConsumer<AccountCreatedNotif>
        , IConsumer<AccountDeletedNotif>, IConsumer<LogoutNotif>

        where T : DatabaseContext

    {
        private readonly Func<IBusControl> _busFetcher;

        protected GlobalConsumer(Func<IBusControl> busFetcher)
        {
            this._busFetcher = busFetcher;
        }
        
        public Task Consume(ConsumeContext<AccountDeletedNotif> context)
        {
            var gUser = context.Message.Packet.User;

            using (var dbContext = new DatabaseContext())
            {
                var user = dbContext.Users.Find(gUser.UserId);

                if (user != null)
                {
                    dbContext.Entry(user).Collection(u => u.Sessions).Load();
                    dbContext.Entry(user).Reference(u => u.UserSecret).Load();

                    user.Title = "Deleted User";
                    user.UserSecret.Email = "";
                    dbContext.Sessions.RemoveRange(user.Sessions);

                    dbContext.SaveChanges();
                }
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SessionCreatedNotif> context)
        {
            using (var dbContext = (DatabaseContext) Activator.CreateInstance<T>())
            {
                var session = context.Message.Packet.Session;
                var user = context.Message.Packet.User;
                var bot = context.Message.Packet.Bot;

                if (user != null)
                {
                    var userSess = (UserSession) session;
                    userSess.User = dbContext.Users.Find(user.UserId);
                }
                else if (bot != null)
                {
                    var botSess = (BotSession) session;
                    botSess.Bot = dbContext.Bots.Find(bot.BotId);
                }

                dbContext.AddRange(session);

                dbContext.SaveChanges();
                
            }
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<UserProfileUpdatedNotif> context)
        {
            using (var dbContext = (DatabaseContext) Activator.CreateInstance<T>())
            {
                var globalUser = context.Message.Packet.User;

                var localUser = dbContext.Users.Find(globalUser.UserId);

                localUser.Title = globalUser.Title;

                dbContext.SaveChanges();
            }
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SessionUpdatedNotif> context)
        {
            var globalSession = context.Message.Packet.Session;

            using (var dbContext = (DatabaseContext) Activator.CreateInstance<T>())
            {
                var session = dbContext.Sessions.Find(globalSession.SessionId);

                session.Token = globalSession.Token;

                dbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<AccountCreatedNotif> context)
        {
            using (var dbContext = (DatabaseContext) Activator.CreateInstance<T>())
            {
                var user = context.Message.Packet.User;
                var userSecret = context.Message.Packet.UserSecret;

                user.UserSecret = userSecret;
                userSecret.User = user;
                
                dbContext.AddRange(user);

                dbContext.SaveChanges();
            }
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ConsolidateLogoutRequest> context)
        {
            var gSession = context.Message.Packet.Session;

            using (var dbContext = (DatabaseContext) Activator.CreateInstance<T>())
            {
                var lSess = dbContext.Sessions.Find(gSession.SessionId);
                if (lSess != null)
                {
                    dbContext.Sessions.RemoveRange(lSess);
                    dbContext.SaveChanges();
                }
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<LogoutNotif> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = dbContext.Sessions.Find(context.Message.Packet.Session.SessionId);

                if (session != null)
                {
                    dbContext.Sessions.Remove(session);
                    dbContext.SaveChanges();
                }
            }
            
            return Task.CompletedTask;
        }
    }
}