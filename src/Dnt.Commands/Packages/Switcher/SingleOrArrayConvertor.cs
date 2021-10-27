using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Dnt.Commands.Packages.Switcher
{
    class SingleOrArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, List<string>>) ||
                   objectType == typeof(Dictionary<string, string>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            return token.Children<JProperty>().ToDictionary(val => val.Name, val =>
            {
                switch (val.Value.Type)
                {
                    case JTokenType.String:
                        return new List<string> { val.Value.Value<string>() };
                    case JTokenType.Array:
                        return val.Value.ToObject<List<string>>();
                    default:
                        throw new InvalidCastException();
                }
            });
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = new JObject();
            var properties = value as Dictionary<string, List<string>>;

            foreach (var property in properties)
            {
                if (property.Value.Count > 1)
                {
                    result.Add(new JProperty(property.Key, property.Value));
                }
                else
                {
                    result.Add(new JProperty(property.Key, property.Value.First()));
                }
            }

            writer.WriteToken(result.CreateReader());
        }
    }
}