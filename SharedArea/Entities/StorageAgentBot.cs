using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SharedArea.Entities
{
    public class StorageAgentBot : StorageAgent
    {
        [JsonProperty("botId")]
        public long? BotId { get; set; }
        [JsonProperty("bot")]
        public virtual Bot Bot { get; set; }

        public StorageAgentBot()
        {
            this.Type = "StorageAgentBot";
        }
    }
}