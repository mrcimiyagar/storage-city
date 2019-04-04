using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SharedArea.Entities
{
    public class App
    {
        [JsonProperty("appId")]
        public long AppId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("creatorId")]
        public long? CreatorId { get; set; }
        [JsonProperty("creator")]
        public virtual User Creator { get; set; }
    }
}