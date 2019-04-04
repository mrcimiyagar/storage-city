using System;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Middles;
using SharedArea.Utils;

namespace CityService
{
    public class NotifConsumer : GlobalConsumer<DatabaseContext>, IConsumer<UpdateUserSecretRequest>
    {
        public NotifConsumer(Func<IBusControl> busFetcher) : base(busFetcher)
        {
            
        }
        
        public async Task Consume(ConsumeContext<UpdateUserSecretRequest> context)
        {
            using (var dbContext = new DatabaseContext())
            {
                var globalUs = context.Message.Packet.UserSecret;

                var us = dbContext.UserSecrets.Find(globalUs.UserSecretId);
                us.Email = globalUs.Email;
                us.User = dbContext.Users.Find(globalUs.UserId);

                dbContext.SaveChanges();

                await context.RespondAsync<UpdateUserSecretResponse>(new
                {
                    Packet = new Packet()
                    {
                        UserSecret = us
                    }
                });
            }
        }
    }
}