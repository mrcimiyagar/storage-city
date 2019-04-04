using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class User
    {
        [Key]
        [JsonProperty("userId")]
        public long UserId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("sessions")]
        public virtual List<Session> Sessions { get; set; }
        [JsonProperty("createdBots")]
        public virtual List<BotCreation> CreatedBots { get; set; }
        [JsonProperty("apps")]
        public virtual List<App> Apps { get; set; }
        [JsonIgnore]
        public virtual UserSecret UserSecret { get; set; }
    }
}