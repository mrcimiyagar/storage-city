using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SharedArea.Entities
{
    public class StorageAgentUser : StorageAgent
    {
        [JsonProperty("userId")]
        public long? UserId { get; set; }
        [JsonProperty("user")]
        public virtual User User { get; set; }

        public StorageAgentUser()
        {
            this.Type = "StorageAgentUser";
        }
    }
}