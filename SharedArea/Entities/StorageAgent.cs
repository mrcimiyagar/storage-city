using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class StorageAgent
    {
        [Key]
        [JsonProperty("storageAgentEntId")]
        public long StorageOwnerEntId { get; set; }
        [JsonProperty("storageId")]
        public long? StorageId { get; set; }
        [JsonProperty("storage")]
        public virtual Storage Storage { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}