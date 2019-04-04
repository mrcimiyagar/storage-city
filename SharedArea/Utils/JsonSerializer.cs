
using System;
using Newtonsoft.Json;

namespace SharedArea.Utils
{
    public static class JsonSerializer
    {
        public static string SerializeObject(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            settings.Converters.Add(new SessionConverter());
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T DeserializeObject<T>(string json)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            settings.Converters.Add(new SessionConverter());
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        
        public static object DeserializeObject(string json, Type type)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            settings.Converters.Add(new SessionConverter());
            return JsonConvert.DeserializeObject(json, type, settings);
        }
    }
}