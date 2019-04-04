using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class Pending
    {
        [Key]
        [JsonProperty("pendingId")]
        public long PendingId { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("verifyCode")]
        public string VerifyCode { get; set; }
    }
}