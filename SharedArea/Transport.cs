using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using SharedArea.Commands;
using SharedArea.Entities;
using SharedArea.Middles;

namespace SharedArea
{
    public static class Transport
    {
        public static void NotifyService<TA>(IBusControl bus, Packet packet, string[] destinations)
            where TA : class
        {
            if (destinations.Length > 0)
            {
                var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" +
                                      SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME
                                      + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);

                bus.GetSendEndpoint(address).Result.Send<TA>(new
                {
                    Packet = packet,
                    Destinations = destinations
                });
            }
        }

        public static void NotifyPacketRouter<TA>(IBusControl bus, Packet packet) where TA : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" +
                                  SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME
                                  + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);

            bus.GetSendEndpoint(address).Result.Send<TA>(new
            {
                Packet = packet,
                Destinations = new string[0]
            });
        }

        public static void NotifyServiceDirectly<TA>(IBusControl bus, Packet packet, string destination)
            where TA : class 
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + destination
                                  + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            
            bus.GetSendEndpoint(address).Result.Send<TA>(new
            {
                Packet = packet
            });
        }

        public static async Task<TB> RequestService<TA, TB>(IBusControl bus, string queueName, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + SharedArea.GlobalVariables
                                      .PACKET_ROUTER_QUEUE_NAME+ SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Packet = packet,
                Destination = queueName
            });
            return result;
        }
        
        public static async Task<TB> DirectService<TA, TB>(IBusControl bus, string queueName, Session session
            , Dictionary<string, string> headers, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + queueName + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                SessionId = session.SessionId,
                Headers = headers,
                Packet = packet
            });
            return result;
        }
        
        public static async Task<TB> DirectService<TA, TB>(IBusControl bus, string queueName
            , Dictionary<string, string> headers, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + queueName + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Headers = headers,
                Packet = packet
            });
            return result;
        }
        
        public static async Task<TB> RequestService<TA, TB>(IBusControl bus, string queueName
            , Dictionary<string, string> headers, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Headers = headers,
                Packet = packet,
                Destination = queueName
            });
            return result;
        }
        
        public static async Task<TB> RequestService<TA, TB>(IBusControl bus, string queueName
            , Session session, Dictionary<string, string> headers, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Headers = headers,
                Packet = packet,
                Destination = queueName,
                SessionId = session.SessionId,
            });
            return result;
        }
                
        public static async Task<TB> RequestService<TA, TB>(IBusControl bus, string queueName
            , Session session, Dictionary<string, string> headers)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Headers = headers,
                Destination = queueName,
                SessionId = session.SessionId,
            });
            return result;
        }
        
        public static async Task<TB> RequestService<TA, TB>(IBusControl bus, string queueName
            , Dictionary<string, string> headers)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + SharedArea.GlobalVariables.PACKET_ROUTER_QUEUE_NAME + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Headers = headers,
                Destination = queueName
            });
            return result;
        }
                
        public static async Task<TB> DirectService<TA, TB>(IBusControl bus, string queueName, Packet packet)
            where TA : class
            where TB : class
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + queueName + SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Packet = packet
            });
            return result;
        }
        
        public static async Task<TB> RequestApiGateway<TA, TB>(IBusControl bus, Packet packet)
            where TA : Request
            where TB : Response
        {
            var address = new Uri(SharedArea.GlobalVariables.RABBITMQ_SERVER_URL + "/" + 
                                  SharedArea.GlobalVariables.API_GATEWAY_QUEUE_NAME +
                                  SharedArea.GlobalVariables.RABBITMQ_SERVER_URL_EXTENSIONS);
            var requestTimeout = TimeSpan.FromSeconds(SharedArea.GlobalVariables.RABBITMQ_REQUEST_TIMEOUT);
            IRequestClient<TA, TB> client = new MessageRequestClient<TA, TB>(bus, address, requestTimeout);
            var result = await client.Request<TA, TB>(new
            {
                Packet = packet
            });
            return result;
        }
    }
}