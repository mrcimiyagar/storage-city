using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class Storage
    {
        [Key]
        [JsonProperty("storageId")]
        public long StorageId { get; set; }
        [JsonProperty("creatorEntId")]
        public long? CreatorEntId { get; set; }
        [JsonProperty("creatorEnt")]
        public virtual StorageAgent CreatorEnt { get; set; }
    }
}