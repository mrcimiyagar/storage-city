using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class Bot
    {
        [Key]
        [JsonProperty("botId")]
        public long BotId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("sessions")]
        public virtual List<Session> Sessions { get; set; }
        [JsonProperty("botSecret")]
        public virtual BotSecret BotSecret { get; set; }
    }
}