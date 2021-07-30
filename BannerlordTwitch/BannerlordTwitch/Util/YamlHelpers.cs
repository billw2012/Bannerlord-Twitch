using System;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Rewards
{
    public static class YamlHelpers
    {
        public static string Serialize(object obj) => CreateDefaultSerializer().Serialize(obj);
        public static object Deserialize(string str, Type type) => CreateDefaultDeserializer().Deserialize(str, type);
        public static T Deserialize<T>(string str) => (T)Deserialize(str, typeof(T));
        
        public static ISerializer CreateDefaultSerializer() => new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .DisableAliases()
            .Build();
        
        public static IDeserializer CreateDefaultDeserializer() => new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        
        public static object ConvertObject(object obj, Type type) => Deserialize(Serialize(obj), type);
        public static T ConvertObject<T>(object obj) => (T)ConvertObject(obj, typeof(T));
    }
}