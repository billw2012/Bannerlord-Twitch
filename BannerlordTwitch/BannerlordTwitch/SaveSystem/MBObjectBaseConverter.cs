using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace BannerlordTwitch.SaveSystem
{
    public sealed class MBObjectBaseConverter : JsonConverter
    {
        private readonly Dictionary<string, IList> references = new();
        private readonly IDataStore dataStore;
        private readonly string key;

        public MBObjectBaseConverter(IDataStore dataStore, string key)
        {
            this.dataStore = dataStore;
            this.key = key;

            if (dataStore.IsLoading)
            {
                Load();
            }
        }

        public override bool CanConvert(Type objectType) => typeof(MBObjectBase).IsAssignableFrom(objectType);

        private struct SavedMBObject
        {
            public int ID { get; set; }
            public string Type { get; set; }

            public override string ToString()
            {
                return $"{nameof(ID)}: {ID}, {nameof(Type)}: {Type}";
            }
        }

        private static string GetTypeID(Type type) => type.Name;
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is MBObjectBase mbObject)
            {
                var type = mbObject.GetType();
                if (!references.TryGetValue(GetTypeID(type), out var typedList))
                {
                    typedList = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                    references.Add(GetTypeID(type), typedList);
                }
                typedList.Add(mbObject);

                // We use the index into the list as the key for this object
                serializer.Serialize(writer, new SavedMBObject  { ID = typedList.Count - 1, Type = GetTypeID(type) });
                return;
            }

            serializer.Serialize(writer, null);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (serializer.Deserialize<SavedMBObject?>(reader) is { } savedMBObject)
            {
                if (!references.TryGetValue(savedMBObject.Type, out var typedList))
                    throw new ($"{savedMBObject} could not be resolved on loading in {key}: list of references for this type doesn't exist");
                if (savedMBObject.ID < 0 || savedMBObject.ID >= typedList.Count)
                    throw new ($"{savedMBObject} could not be resolved on loading in {key}: ID was out of range");
                return typedList[savedMBObject.ID];
            }
            return null;
        }
        
        public void Save()
        {
            var types = references.Keys.ToList();
            dataStore.SyncData($"{key}_refs_type_list", ref types);
            
            foreach ((string type, var typedList) in references)
            {
                var listToSave = typedList;
                dataStore.SyncData($"{key}_ref_list_{type}", ref listToSave);
            }
        }

        private void Load()
        {
            List<string> types = null;
            dataStore.SyncData($"{key}_refs_type_list", ref types);
            if (types != null)
            {
                foreach (string type in types)
                {
                    IList typedList = null;
                    dataStore.SyncData($"{key}_ref_list_{type}", ref typedList);
                    if (typedList != null)
                    {
                        references.Add(type, typedList);
                    }
                }
            }
        }
    }
}