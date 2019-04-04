
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiGateway.DbContexts;
using MassTransit;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Entities;

namespace ApiGateway.Consumers
{
    public class ApiGatewayConsumer : IConsumer<SessionCreatedNotif>, IConsumer<DeleteSessionsNotif>
        , IConsumer<LogoutNotif>, IConsumer<ConsolidateSessionRequest>
    {
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
        
        public Task Consume(ConsumeContext<SessionCreatedNotif> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = context.Message.Packet.Session;
            
                if (session is UserSession us)
                {
                    us.User = null;
                    us.UserId = null;
                }
                else if (session is BotSession bs)
                {
                    bs.Bot = null;
                    bs.BotId = null;
                }

                dbContext.Sessions.Add(session);

                dbContext.SaveChanges();
            }
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<DeleteSessionsNotif> context)
        {
            var gSessions = context.Message.Packet.Sessions;

            using (var dbContext = new DatabaseContext())
            {
                var lSessions = new List<Session>();
                foreach (var gSession in gSessions)
                    lSessions.Add(dbContext.Sessions.Find(gSession.SessionId));

                dbContext.Sessions.RemoveRange(lSessions);

                dbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<ConsolidateSessionRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var session = context.Message.Packet.Session;
                
                if (session is UserSession us)
                {
                    us.User = null;
                    us.UserId = null;
                }
                else if (session is BotSession bs)
                {
                    bs.Bot = null;
                    bs.BotId = null;
                }
                
                dbContext.Sessions.Add(session);

                dbContext.SaveChanges();
            }

            await context.RespondAsync(new ConsolidateSessionResponse());
        }
    }
}