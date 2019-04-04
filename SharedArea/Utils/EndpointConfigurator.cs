using GreenPipes;
using MassTransit.ConsumeConfigurators;
using MassTransit.RabbitMqTransport;

namespace SharedArea.Utils
{
    public static class EndpointConfigurator
    {
        public static void ConfigEndpoint(IRabbitMqReceiveEndpointConfigurator ep)
        {
            ep.UseConcurrencyLimit(SharedArea.GlobalVariables.PIPE_COUNT);
            ep.PrefetchCount = SharedArea.GlobalVariables.PIPE_COUNT;
        }

        public static void ConfigConsumer<T>(IConsumerConfigurator<T> cons) where T : class
        {
            cons.UseConcurrencyLimit(SharedArea.GlobalVariables.PIPE_COUNT);
        }
    }
}