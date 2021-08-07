using Newtonsoft.Json;

using System;

using TaleWorlds.ObjectSystem;

namespace BannerlordTwitch.SaveSystem
{
    public sealed class MBGUIDConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(MBGUID) || objectType == typeof(MBGUID?);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is MBGUID)
            {
                throw new Exception($"MBGUIDs should not be saved, they are not stable references to objects");
            }
            serializer.Serialize(writer, null);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return null;
        }
    }
}