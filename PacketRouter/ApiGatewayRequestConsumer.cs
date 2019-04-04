using System;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands;
using SharedArea.Commands.App;
using SharedArea.Commands.Auth;
using SharedArea.Commands.Bot;
using SharedArea.Commands.File;
using SharedArea.Commands.Internal.Requests;
using SharedArea.Commands.Internal.Responses;
using SharedArea.Commands.User;

namespace PacketRouter
{
    public class ApiGatewayRequestConsumer : IConsumer<RegisterRequest>, IConsumer<VerifyRequest>,
        IConsumer<LogoutRequest>, IConsumer<DeleteAccountRequest>
        , IConsumer<GetBotsRequest>, IConsumer<AddBotToRoomRequest>, IConsumer<GetBotStoreContentRequest>,
        IConsumer<UpdateWorkershipRequest>
        , IConsumer<GetCreatedBotsRequest>, IConsumer<GetSubscribedBotsRequest>, IConsumer<SubscribeBotRequest>,
        IConsumer<CreateBotRequest>, IConsumer<GetBotRequest>, IConsumer<UpdateBotProfileRequest>
        , IConsumer<GetWorkershipsRequest>, IConsumer<SearchBotsRequest>, IConsumer<RemoveBotFromRoomRequest>
        , IConsumer<WriteToFileRequest>, IConsumer<GetFileSizeRequest>, IConsumer<DownloadFileRequest>
        , IConsumer<UpdateUserProfileRequest>, IConsumer<GetMeRequest>, IConsumer<GetUserByIdRequest>,
        IConsumer<SearchUsersRequest>, IConsumer<CreateFileRequest>, IConsumer<CreateAppRequest>
    {
        private async Task<TB> ForwardPacket<TA, TB>(ConsumeContext<TA> context)
            where TA : Request
            where TB : Response
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + context.Message.Destination +
                                  SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(Program.Bus, address, requestTimeout);

            TB result;

            var request = context.Message;

            result = await client.Request<TA, TB>(new
            {
                SessionId = request.SessionId,
                SessionVersion = request.SessionVersion,
                Headers = request.Headers,
                Packet = request.Packet
            });

            return result;
        }

        public Task Consume(ConsumeContext<RegisterRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<RegisterRequest, RegisterResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<VerifyRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<VerifyRequest, VerifyResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<LogoutRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<LogoutRequest, LogoutResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<DeleteAccountRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<DeleteAccountRequest, DeleteAccountResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetBotsRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetBotsRequest, GetBotsResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<AddBotToRoomRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<AddBotToRoomRequest, AddBotToRoomResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetBotStoreContentRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetBotStoreContentRequest, GetBotStoreContentResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<UpdateWorkershipRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<UpdateWorkershipRequest, UpdateWorkershipResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetCreatedBotsRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetCreatedBotsRequest, GetCreatedBotsResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetSubscribedBotsRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetSubscribedBotsRequest, GetSubscribedBotsResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SubscribeBotRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<SubscribeBotRequest, SubscribeBotResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<CreateBotRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<CreateBotRequest, CreateBotResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetBotRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetBotRequest, GetBotResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<UpdateBotProfileRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<UpdateBotProfileRequest, UpdateBotProfileResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetWorkershipsRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetWorkershipsRequest, GetWorkershipsResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SearchBotsRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<SearchBotsRequest, SearchBotsResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<RemoveBotFromRoomRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<RemoveBotFromRoomRequest, RemoveBotFromRoomResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<WriteToFileRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<WriteToFileRequest, WriteToFileResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetFileSizeRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetFileSizeRequest, GetFileSizeResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<DownloadFileRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<DownloadFileRequest, DownloadFileResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<UpdateUserProfileRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<UpdateUserProfileRequest, UpdateUserProfileResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetMeRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetMeRequest, GetMeResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<GetUserByIdRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<GetUserByIdRequest, GetUserByIdResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SearchUsersRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<SearchUsersRequest, SearchUsersResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<CreateFileRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<CreateFileRequest, CreateFileResponse>(context)));
            
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<CreateAppRequest> context)
        {
            Task.Run(async () => await context.RespondAsync(await ForwardPacket<CreateAppRequest, CreateAppResponse>(context)));
            
            return Task.CompletedTask;   
        }
    }
}