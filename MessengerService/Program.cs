using System;
using Bugsnag;
using GreenPipes;
using MassTransit;
using Newtonsoft.Json;
using SharedArea.DbContexts;
using SharedArea.Utils;

namespace MessengerService
{
    class Program
    {
        public static IBusControl Bus { get; set; }
        
        static void Main(string[] args)
        {
            Logger.Setup();

            using (var dbContext = new DatabaseContext())
            {
                DatabaseConfig.ConfigDatabase(dbContext);
            }
            
            Bus = MassTransit.Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_PATH), h =>
                {
                    h.Username(SharedArea.GlobalVariables.RABBITMQ_USERNAME);
                    h.Password(SharedArea.GlobalVariables.RABBITMQ_PASSWORD);
                });
                sbc.UseJsonSerializer();
                sbc.ConfigureJsonSerializer(options =>
                {
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.NullValueHandling = NullValueHandling.Ignore;
                    return options;
                });
                sbc.ConfigureJsonDeserializer(options =>
                {
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.NullValueHandling = NullValueHandling.Ignore;
                    return options;
                });
                sbc.ReceiveEndpoint(host, SharedArea.GlobalVariables.MESSENGER_QUEUE_NAME, ep =>
                {
                    EndpointConfigurator.ConfigEndpoint(ep);
                    ep.Consumer(() => new MessengerConsumer(), EndpointConfigurator.ConfigConsumer);
                    ep.Consumer(() => new NotifConsumer(() => Program.Bus), EndpointConfigurator.ConfigConsumer);
                });
            });
            
            var bugsnag = new Bugsnag.Client(new Configuration("cede7e5400965370490a969c22544916"));

            Program.Bus.ConnectSendObserver(new SendObserver(bugsnag));
            Program.Bus.ConnectConsumeObserver(new ConsumeObserver(bugsnag));
            Program.Bus.ConnectReceiveObserver(new ReceiveObserver(bugsnag));

            Bus.Start();
            
            Console.WriteLine("Bus loaded");
        }
    }
}