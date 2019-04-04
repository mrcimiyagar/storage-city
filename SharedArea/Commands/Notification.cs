using SharedArea.Middles;

namespace SharedArea.Commands
{
    public class Notification
    {
        public Packet Packet { get; set; }
        public string[] Destinations { get; set; }
    }
}