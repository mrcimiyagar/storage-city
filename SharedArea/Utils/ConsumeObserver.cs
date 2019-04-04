using System;
using System.Threading.Tasks;
using MassTransit;

namespace SharedArea.Utils
{
    public class ConsumeObserver : IConsumeObserver
    {
        private readonly Bugsnag.IClient _bugsnag;

        public ConsumeObserver(Bugsnag.IClient bugsnag)
        {
            this._bugsnag = bugsnag;
        }
        
        Task IConsumeObserver.PreConsume<T>(ConsumeContext<T> context)
        {            
            //var content = JsonSerializer.SerializeObject(context.Message);
            //Logger.Log("Microservices Bus", $"== Consuming ===========================================" + Environment.NewLine +
                //$"Message-type: {context.Message}, " + Environment.NewLine +
                //$"Content: " + (content.Length > 500 ? "body too big to be printed" : content) + Environment.NewLine +
                //$"Source-address: {context.SourceAddress}, " + Environment.NewLine +
                //$"Destination-address: {context.DestinationAddress}, " + Environment.NewLine +
                //$"Fault-address: {context.FaultAddress}" + Environment.NewLine +
                //$"========================================================" + Environment.NewLine);
                Logger.Log("Microservices Bus", "Consuming " + context.Message.GetType().ToString());
            return Task.CompletedTask;
        }

        Task IConsumeObserver.PostConsume<T>(ConsumeContext<T> context)
        {
            // called after the consumer's Consume method is called
            // if an exception was thrown, the ConsumeFault method is called instead
            return Task.CompletedTask;
        }

        Task IConsumeObserver.ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
        {   
            _bugsnag?.Notify(exception);
            //var content = JsonSerializer.SerializeObject(context.Message);
            //Logger.Log("Microservices Bus", $"== Error Consuming =====================================" + Environment.NewLine +
                              //$"Message-type: {context.Message}, " + Environment.NewLine +
                              //$"Content: " + (content.Length > 500 ? "body too big to be printed" : content) + Environment.NewLine +
                              //$"Source-address: {context.SourceAddress}, " + Environment.NewLine +
                              //$"Destination-address: {context.DestinationAddress}, " + Environment.NewLine +
                              //$"Fault-address: {context.FaultAddress}" + Environment.NewLine +
                              //$"" + Environment.NewLine +
                              //$"" + exception.ToString() + Environment.NewLine +
                              //$"========================================================");
            Logger.Log("Microservices Bus", Environment.NewLine + "Exception while consuming : " + context.Message.GetType().ToString()
                                            + Environment.NewLine + exception.ToString() + Environment.NewLine);
            return Task.CompletedTask;
        }
    }
}