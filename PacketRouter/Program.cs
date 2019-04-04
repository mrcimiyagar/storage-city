using System;
using Bugsnag;
using MassTransit;
using Newtonsoft.Json;
using SharedArea.Utils;

namespace PacketRouter
{
    class Program
    {
        public static IBusControl Bus { get; set; }
        
        static void Main(string[] args)
        {
            Logger.Setup();
                        
            Program.Bus = MassTransit.Bus.Factory.CreateUsingRabbitMq(sbc =>
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
                    options.Converters.Add(new SessionConverter());
                    return options;
                });
                sbc.ConfigureJsonDeserializer(options =>
                {
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.NullValueHandling = NullValueHandling.Ignore;
                    options.Converters.Add(new SessionConverter());
                    return options;
                });
                sbc.ReceiveEndpoint(host, SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME, ep =>
                {
                    EndpointConfigurator.ConfigEndpoint(ep);
                    ep.Consumer<PacketRouterConsumer>(EndpointConfigurator.ConfigConsumer);
                    ep.Consumer<ApiGatewayRequestConsumer>(EndpointConfigurator.ConfigConsumer);
                });
            });
            
            var bugsnag = new Bugsnag.Client(new Configuration("96b2c0079a181c03cd33c518c8f8e90a"));

            Program.Bus.ConnectSendObserver(new SendObserver(bugsnag));
            Program.Bus.ConnectConsumeObserver(new ConsumeObserver(bugsnag));
            Program.Bus.ConnectReceiveObserver(new ReceiveObserver(bugsnag));
            
            Program.Bus.Start();
            
            Console.WriteLine("Bus loaded");
        }
    }
}