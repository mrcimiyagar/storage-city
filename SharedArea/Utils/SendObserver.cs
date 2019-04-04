using System;
using System.Dynamic;
using System.Threading.Tasks;
using MassTransit;

namespace SharedArea.Utils
{
    public class SendObserver : ISendObserver
    {
        private readonly Bugsnag.IClient _bugsnag;

        public SendObserver(Bugsnag.IClient bugsnag)
        {
            this._bugsnag = bugsnag;
        }
        
        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            /*var content = JsonSerializer.SerializeObject(context.Message);
            Logger.Log("Microservices Bus", $"== Sending =============================================" + Environment.NewLine +
                              $"Message-type: {context.Message}, " + Environment.NewLine +
                              //$"Content: " + (content.Length > 500 ? "body too big to be printed" : content) + Environment.NewLine +
                              //$"Source-address: {context.SourceAddress}, " + Environment.NewLine +
                              //$"Destination-address: {context.DestinationAddress}, " + Environment.NewLine +
                              //$"Fault-address: {context.FaultAddress}" + Environment.NewLine +
                              $"========================================================");*/
            Logger.Log("Microservices Bus", "Sending " + context.Message.GetType().ToString());
            
            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            // called just after a message it sent to the transport and acknowledged (RabbitMQ)
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _bugsnag?.Notify(exception);
            /*var content = JsonSerializer.SerializeObject(context.Message);
            Logger.Log("Microservices Bus", $"== Error Sending =======================================" + Environment.NewLine +
                              $"Message-type: {context.Message}, " + Environment.NewLine +
                              //$"Content: " + (content.Length > 500 ? "body too big to be printed" : content) + Environment.NewLine +
                              //$"Source-address: {context.SourceAddress}, " + Environment.NewLine +
                              //$"Destination-address: {context.DestinationAddress}, " + Environment.NewLine +
                              //$"Fault-address: {context.FaultAddress}" + Environment.NewLine+
                              $"" + Environment.NewLine +
                              $"" + exception.ToString() + Environment.NewLine +
                              $"========================================================");*/
            Logger.Log("Microservices Bus", Environment.NewLine + "Exception while sending " + context.Message.GetType().ToString() + " : " 
                                            + Environment.NewLine + exception.ToString() + Environment.NewLine);
            return Task.CompletedTask;
        }
    }
}