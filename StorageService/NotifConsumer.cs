using System;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Middles;
using SharedArea.Utils;

namespace StorageService
{
    public class NotifConsumer : GlobalConsumer<DatabaseContext>, IConsumer<AccountCreatedWithBackRequest>
    {
        public NotifConsumer(Func<IBusControl> busFetcher) : base(busFetcher)
        {
            
        }
        
        public async Task Consume(ConsumeContext<AccountCreatedWithBackRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var user = context.Message.Packet.User;
                var userSecret = context.Message.Packet.UserSecret;

                user.UserSecret = userSecret;
                userSecret.User = user;

                dbContext.AddRange(user);

                dbContext.SaveChanges();

                await context.RespondAsync(new AccountCreatedWithBackResponse()
                {
                    Packet = new Packet()
                });
            }
        }
    }
}