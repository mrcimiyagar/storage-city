using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenPipes.Filters.Log;
using MassTransit;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace SharedArea.Utils
{
    public class MessageFormatter
    {
        public static async Task<string> Formatter(LogContext<ConsumeContext> context)
        {
            try
            {
                byte[] content = context.Context.ReceiveContext.GetBody();
                var str = Encoding.UTF8.GetString(content);
                var definition = new { message = new object() };
                var ctx = JsonConvert.DeserializeAnonymousType(str, definition);
                return await Task.Run(() =>
                    $"========================================================" + Environment.NewLine +
                    $"Start-Time: {context.StartTime}, " + Environment.NewLine +
                    $"Message-type: {context.Context.SupportedMessageTypes.FirstOrDefault()}, " + Environment.NewLine +
                    $"Content: {JsonConvert.SerializeObject(ctx.message)}" + Environment.NewLine +
                    $"Source-address: {context.Context.SourceAddress}, " + Environment.NewLine +
                    $"Destination-address: {context.Context.DestinationAddress}, " + Environment.NewLine +
                    $"Fault-address: {context.Context.FaultAddress}" + Environment.NewLine +
                    $"========================================================" + Environment.NewLine);
            }
            catch (Exception)
            {
                return await Task.Run(() =>
                    $"========================================================" + Environment.NewLine +
                    $"Start-Time: {context.StartTime}, " + Environment.NewLine +
                    $"Message-type: {context.Context.SupportedMessageTypes.FirstOrDefault()}, " + Environment.NewLine +
                    $"Source-address: {context.Context.SourceAddress}, " + Environment.NewLine +
                    $"Destination-address: {context.Context.DestinationAddress}, " + Environment.NewLine +
                    $"Fault-address: {context.Context.FaultAddress}" + Environment.NewLine +
                    $"========================================================" + Environment.NewLine);
            }
        }
    }
}