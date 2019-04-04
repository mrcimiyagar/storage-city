using System.Collections.Generic;
using SharedArea.Middles;

namespace SharedArea.Commands
{
    public class Request
    {
        public long SessionId { get; set; }
        public long SessionVersion { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Packet Packet { get; set; }
        public string Destination { get; set; }
    }
}