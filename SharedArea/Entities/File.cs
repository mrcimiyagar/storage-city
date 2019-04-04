using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SharedArea.Entities
{
    public class File
    {
        [Key]
        [JsonProperty("fileId")]
        public long FileId { get; set; }
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        [JsonProperty("size")]
        public long Size { get; set; }
        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }
        [JsonIgnore]
        public long? UploaderId { get; set; }
        [JsonIgnore]
        public virtual StorageAgent Uploader { get; set; }
    }
}