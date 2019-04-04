using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedArea.Entities;

namespace SharedArea.Utils
{
    public class SessionConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Session));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            switch (jo["type"].Value<string>())
            {
                case "UserSession":
                    return jo.ToObject<UserSession>(serializer);
                case "BotSession":
                    return jo.ToObject<BotSession>(serializer);
                default:
                {
                    return jo.ToObject<Session>(Newtonsoft.Json.JsonSerializer.CreateDefault());
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