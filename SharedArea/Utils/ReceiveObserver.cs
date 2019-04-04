using System;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Newtonsoft.Json;

namespace SharedArea.Utils
{
    public class ReceiveObserver : IReceiveObserver
    {
        private readonly Bugsnag.IClient _bugsnag;

        public ReceiveObserver(Bugsnag.IClient bugsnag)
        {
            this._bugsnag = bugsnag;
        }
        
        public Task PreReceive(ReceiveContext context)
        {
            byte[] content = context.GetBody();
            var str = Encoding.UTF8.GetString(content);
            var definition = new { message = new object() };
            var ctx = JsonConvert.DeserializeAnonymousType(str, definition);
            /*var body = JsonSerializer.SerializeObject(ctx.message);
            Logger.Log("Microservices Bus",
                $"== Receiving ===========================================" + Environment.NewLine +
                $"Message-type: {ctx.message.GetType()}, " + Environment.NewLine +
                //$"Content: " + (body.Length > 500 ? "body too big to be printed" : body) + Environment.NewLine +
                $"========================================================" + Environment.NewLine);*/
            Logger.Log("Microservices Bus", "Receiving " + ctx.message.GetType().ToString());
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context)
        {
            // called after the message has been received and processed
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            // called when the message was consumed, once for each consumer
            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan elapsed, string consumerType, Exception exception) where T : class
        {
            // called when the message is consumed but the consumer throws an exception
            return Task.CompletedTask;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            _bugsnag?.Notify(exception);
            byte[] content = context.GetBody();
            var str = Encoding.UTF8.GetString(content);
            var definition = new { message = new object() };
            var ctx = JsonConvert.DeserializeAnonymousType(str, definition);
            /*var body = JsonSerializer.SerializeObject(ctx.message);
            Logger.Log("Microservices Bus",
                $"== Error Receiving =====================================" + Environment.NewLine +
                $"Message-type: {ctx.message.GetType()}, " + Environment.NewLine +
                //$"Content: " + (body.Length > 500 ? "body too big to be printed" : body) + Environment.NewLine +
                $"" + Environment.NewLine +
                $"" + exception.ToString() + Environment.NewLine +
                $"========================================================");*/
            Logger.Log("Microservices Bus", Environment.NewLine + "Exception while receiving " + ctx.message.GetType().ToString() + " : "
                                            + Environment.NewLine + exception.ToString() + Environment.NewLine);
            return Task.CompletedTask;
        }
    }
}