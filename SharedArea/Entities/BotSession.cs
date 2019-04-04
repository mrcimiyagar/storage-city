namespace SharedArea.Entities
{
    public class BotSession : Session
    {
        public long? BotId { get; set; }
        public virtual Bot Bot { get; set; }

        public BotSession()
        {
            this.Type = "BotSession";
        }
    }
}