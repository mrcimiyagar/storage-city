
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class Session
    {
        [Key]
        [JsonProperty("sessionId")]
        public long SessionId { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}