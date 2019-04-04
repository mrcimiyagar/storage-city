using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class BotSecret
    {
        [Key]
        [JsonProperty("botSecretId")]
        public long BotSecretId { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("creatorId")]
        public long? CreatorId { get; set; }
        [JsonProperty("creator")]
        public virtual User Creator { get; set; }
        [JsonProperty("botId")]
        public long? BotId { get; set; }
        [JsonProperty("bot")]
        public virtual Bot Bot { get; set; }
    }
}