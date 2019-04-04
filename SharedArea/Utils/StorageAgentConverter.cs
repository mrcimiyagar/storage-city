using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedArea.Entities;

namespace SharedArea.Utils
{
    public class StorageAgentConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(StorageAgent));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            switch (jo["type"].Value<string>())
            {
                case "StorageAgentUser":
                    return jo.ToObject<StorageAgentUser>(serializer);
                case "StorageAgentBot":
                    return jo.ToObject<StorageAgentBot>(serializer);
                default:
                {
                    return jo.ToObject<StorageAgent>(Newtonsoft.Json.JsonSerializer.CreateDefault());
                }
            }
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}