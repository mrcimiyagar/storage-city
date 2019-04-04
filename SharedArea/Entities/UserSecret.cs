using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class UserSecret
    {
        [Key]
        [JsonProperty("userSecretId")]
        public long UserSecretId { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("userId")]
        public long? UserId { get; set; }
        [JsonProperty("user")]
        public virtual User User { get; set; }
    }
}