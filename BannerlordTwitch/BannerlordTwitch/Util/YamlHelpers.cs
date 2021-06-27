using System;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Rewards
{
    public static class YamlHelpers
    {
        public static object ConvertObject(object obj, Type type) =>
            new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize(
                    new SerializerBuilder()
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                        .Build()
                        .Serialize(obj),
                    type);
        
        public static T ConvertObject<T>(object obj) =>
            (T) new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize(
                    new SerializerBuilder()
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                        .Build()
                        .Serialize(obj),
                    typeof(T));
    }
}