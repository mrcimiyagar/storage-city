using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class BotCreation
    {
        [Key]
        [JsonProperty("botCreationId")]
        public long BotCreationId { get; set; }
        [JsonProperty("botId")]
        public long? BotId { get; set; }
        [JsonProperty("bot")]
        public virtual Bot Bot { get; set; }
        [JsonProperty("creatorId")]
        public long? CreatorId { get; set; }
        [JsonProperty("creator")]
        public virtual User Creator { get; set; }
    }
}