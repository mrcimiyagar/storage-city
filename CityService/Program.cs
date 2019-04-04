using System;
using Bugsnag;
using MassTransit;
using Newtonsoft.Json;
using SharedArea.DbContexts;
using SharedArea.Utils;

namespace CityService
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
                sbc.ReceiveEndpoint(host, SharedArea.GlobalVariables.CITY_QUEUE_NAME, ep =>
                {
                    EndpointConfigurator.ConfigEndpoint(ep);
                    ep.Consumer(() => new CityConsumer(), EndpointConfigurator.ConfigConsumer);
                    ep.Consumer(() => new NotifConsumer(() => Program.Bus), EndpointConfigurator.ConfigConsumer);
                });
            });
            
            var bugsnag = new Bugsnag.Client(new Configuration("1051163ab2c5a802419b25cb4e51710b"));

            Program.Bus.ConnectSendObserver(new SendObserver(bugsnag));
            Program.Bus.ConnectConsumeObserver(new ConsumeObserver(bugsnag));
            Program.Bus.ConnectReceiveObserver(new ReceiveObserver(bugsnag));

            Bus.Start();
            
            Console.WriteLine("Bus loaded");
        }
    }
}