
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands.Internal.Notifications;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Middles;

namespace PacketRouter
{
    public class PacketRouterConsumer : IConsumer<UserCreatedNotif>, IConsumer<ComplexCreatedNotif>
        , IConsumer<RoomCreatedNotif>, IConsumer<MembershipCreatedNotif>, IConsumer<SessionCreatedNotif>
        , IConsumer<UserProfileUpdatedNotif>, IConsumer<ComplexProfileUpdatedNotif>, IConsumer<ComplexDeletionNotif>
        , IConsumer<RoomProfileUpdatedNotif>, IConsumer<ContactCreatedNotif>, IConsumer<InviteCreatedNotif>
        , IConsumer<InviteCancelledNotif>, IConsumer<InviteAcceptedNotif>, IConsumer<InviteIgnoredNotif>
        , IConsumer<BotProfileUpdatedNotif>, IConsumer<BotSubscribedNotif>, IConsumer<BotCreatedNotif>
        , IConsumer<PhotoCreatedNotif>, IConsumer<AudioCreatedNotif>, IConsumer<VideoCreatedNotif>
        , IConsumer<SessionUpdatedNotif>, IConsumer<RoomDeletionNotif>, IConsumer<WorkershipCreatedNotif>
        , IConsumer<WorkershipUpdatedNotif>, IConsumer<WorkershipDeletedNotif>, IConsumer<AccountCreatedNotif>
        , IConsumer<LogoutNotif>, IConsumer<AccountDeletedNotif>

        , IConsumer<PutUserRequest>, IConsumer<PutComplexRequest>, IConsumer<PutRoomRequest>
        , IConsumer<PutMembershipRequest>, IConsumer<PutSessionRequest>, IConsumer<UpdateUserSecretRequest>
        , IConsumer<PutServiceMessageRequest>, IConsumer<ContactCreatedWithBackRequest>
        , IConsumer<ConsolidateSessionRequest>
        , IConsumer<MakeAccountRequest>, IConsumer<ConsolidateDeleteAccountRequest>
        , IConsumer<AccountCreatedWithBackRequest>, IConsumer<ComplexCreatedWithBackRequest>
        , IConsumer<RoomCreatedWithBackRequest>, IConsumer<GetComplexWorkersRequest>, IConsumer<BotCreatedWithBackRequest>
        , IConsumer<ModuleCreatedWithBackRequest>, IConsumer<GetModuleServerAddressRequest>
    {
        public async Task Consume(ConsumeContext<GetModuleServerAddressRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<GetModuleServerAddressRequest, GetModuleServerAddressResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);

            await context.RespondAsync(result);
        }
        
        public async Task Consume(ConsumeContext<BotCreatedWithBackRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<BotCreatedWithBackRequest, BotCreatedWithBackResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);

            await context.RespondAsync(result);
        }
        
        public async Task Consume(ConsumeContext<ModuleCreatedWithBackRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<ModuleCreatedWithBackRequest, ModuleCreatedWithBackResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);

            await context.RespondAsync(result);
        }
        
        public Task Consume(ConsumeContext<AccountDeletedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<AccountDeletedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }
        
        public Task Consume(ConsumeContext<UserCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<UserCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ComplexCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<ComplexCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<RoomCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<RoomCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<MembershipCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<MembershipCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SessionCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<SessionCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<UserProfileUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<UserProfileUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ComplexProfileUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<ComplexProfileUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ComplexDeletionNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<ComplexDeletionNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<RoomProfileUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<RoomProfileUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ContactCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<ContactCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<InviteCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<InviteCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<InviteCancelledNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<InviteCancelledNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<InviteAcceptedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<InviteAcceptedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<InviteIgnoredNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<InviteIgnoredNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<BotProfileUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<BotProfileUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<BotSubscribedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<BotSubscribedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<BotCreatedNotif> context)
        {
            SharedArea.Transport.NotifyServiceDirectly<SessionCreatedNotif>(
                Program.Bus,
                new Packet() {Session = context.Message.Packet.Bot.Sessions.FirstOrDefault()},
                SharedArea.GlobalVariables.API_GATEWAY_QUEUE_NAME);
            
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<BotCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<PhotoCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<PhotoCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<AudioCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<AudioCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<VideoCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<VideoCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SessionUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<SessionUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<RoomDeletionNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<RoomDeletionNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<WorkershipCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<WorkershipCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<WorkershipUpdatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<WorkershipUpdatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<WorkershipDeletedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<WorkershipDeletedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<AccountCreatedNotif> context)
        {
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<AccountCreatedNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }
        
        public Task Consume(ConsumeContext<LogoutNotif> context)
        {
            SharedArea.Transport.NotifyServiceDirectly<LogoutNotif>(
                Program.Bus,
                context.Message.Packet,
                SharedArea.GlobalVariables.API_GATEWAY_QUEUE_NAME);
            
            foreach (var destination in context.Message.Destinations)
            {
                SharedArea.Transport.NotifyServiceDirectly<LogoutNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    destination);
            }

            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<PutUserRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutUserRequest, PutUserResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<PutComplexRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutComplexRequest, PutComplexResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<PutRoomRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutRoomRequest, PutRoomResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<PutMembershipRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutMembershipRequest, PutMembershipResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<PutSessionRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutSessionRequest, PutSessionResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<UpdateUserSecretRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<UpdateUserSecretRequest, UpdateUserSecretResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<PutServiceMessageRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<PutServiceMessageRequest, PutServiceMessageResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<ContactCreatedWithBackRequest> context)
        {
            var result =
                await SharedArea.Transport.DirectService<ContactCreatedWithBackRequest, ContactCreatedWithBackResponse>(
                    Program.Bus,
                    context.Message.Destination,
                    context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<MakeAccountRequest> context)
        {
            var result = await SharedArea.Transport.DirectService<MakeAccountRequest, MakeAccountResponse>(
                Program.Bus,
                context.Message.Destination,
                context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<ConsolidateSessionRequest> context)
        {
            await context.RespondAsync(new ConsolidateSessionResponse());
        }

        public async Task Consume(ConsumeContext<ConsolidateDeleteAccountRequest> context)
        {
            if (context.Message.Destination == "")
            {
                SharedArea.Transport.NotifyServiceDirectly<DeleteSessionsNotif>(
                    Program.Bus,
                    context.Message.Packet,
                    SharedArea.GlobalVariables.API_GATEWAY_QUEUE_NAME);
                
                await context.RespondAsync(new ConsolidateDeleteAccountResponse());
            }
            else
            {
                var result = await SharedArea.Transport
                    .DirectService<ConsolidateDeleteAccountRequest, ConsolidateDeleteAccountResponse>(
                        Program.Bus,
                        context.Message.Destination,
                        context.Message.Packet);
                await context.RespondAsync(result);
            }
        }

        public async Task Consume(ConsumeContext<AccountCreatedWithBackRequest> context)
        {
            var result = await SharedArea.Transport
                .DirectService<AccountCreatedWithBackRequest, AccountCreatedWithBackResponse>(
                    Program.Bus,
                    context.Message.Destination,
                    context.Message.Packet);
            await context.RespondAsync(result);
        }
        
        public async Task Consume(ConsumeContext<ComplexCreatedWithBackRequest> context)
        {
            var result = await SharedArea.Transport
                .DirectService<ComplexCreatedWithBackRequest, ComplexCreatedWithBackResponse>(
                    Program.Bus,
                    context.Message.Destination,
                    context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<RoomCreatedWithBackRequest> context)
        {
            var result = await SharedArea.Transport
                .DirectService<RoomCreatedWithBackRequest, RoomCreatedWithBackResponse>(
                    Program.Bus,
                    context.Message.Destination,
                    context.Message.Packet);
            await context.RespondAsync(result);
        }

        public async Task Consume(ConsumeContext<GetComplexWorkersRequest> context)
        {
            var result = await SharedArea.Transport
                .DirectService<GetComplexWorkersRequest, GetComplexWorkersResponse>(
                    Program.Bus,
                    context.Message.Destination,
                    context.Message.Packet);
            await context.RespondAsync(result);
        }
    }
}