using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace BannerlordTwitch.SaveSystem
{
    public sealed class MBObjectBaseConverter : JsonConverter
    {
        private readonly Dictionary<MBGUID, MBObjectBase> references = new();
        private readonly string key;

        //private readonly List<Type> mbObjectDerivedTypes;

        public MBObjectBaseConverter(string key)
        {
            this.key = key;
            // mbObjectDerivedTypes = AppDomain.CurrentDomain.GetAssemblies()
            //     .SelectMany(a => a.GetTypes())
            //     .Where(t => t.IsSubclassOf(typeof(MBObjectBase)))
            //     .ToList();
        }

        public override bool CanConvert(Type objectType) => typeof(MBObjectBase).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is MBObjectBase mbObject)
            {
                references[mbObject.Id] = mbObject;

                serializer.Serialize(writer, mbObject.Id);
                return;
            }

            serializer.Serialize(writer, null);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (serializer.Deserialize<MBGUID?>(reader) is { } mbguid)
            {
                return FindCampaignObjectManager(mbguid, objectType);
            }
            return null;
        }
        
        public void Save(IDataStore dataStore)
        {
            var refs = references.Values.ToList();

            var concreteTypeLists = refs.GroupBy(r => r.GetType())
                .Select(g =>
                {
                    // Create a list with the concrete type of the MBObject so we can save it without needing
                    // to declare new savable types
                    var listInstance = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(g.Key));
                    foreach(var o in g)
                    {
                        listInstance.Add(o);
                    }
                    return (g.Key, listInstance);
                });

            foreach (var (type, list) in concreteTypeLists)
            {
                var listToSave = list;
                dataStore.SyncData($"{key}_{type.Name}_refs", ref listToSave);
            }
        }
        
#if e159 || e1510
        private static MBObjectBase FindCampaignObjectManager(MBGUID id, Type type = null)
        {
            try
            {
                return MBObjectManager.Instance.GetObject(id);
            }
            catch (Exception e) when (e is MBTypeNotRegisteredException)
            {
                return null;
            }
        }
#else
        private static readonly AccessTools.FieldRef<CampaignObjectManager, object[]> CampaignObjectTypeObjects =
            AccessTools.FieldRefAccess<CampaignObjectManager, object[]>("_objects");
        private static readonly Type ICampaignObjectTypeType =
            AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignObjectManager.ICampaignObjectType");
        private static readonly MethodInfo ObjectClassGetter =
            AccessTools.PropertyGetter(ICampaignObjectTypeType!, "ObjectClass");

        private static MBObjectBase FindCampaignObjectManager(MBGUID id, Type type)
        {
            foreach (var cot in CampaignObjectTypeObjects?.Invoke(Campaign.Current.CampaignObjectManager) ?? Array.Empty<object>())
            {
                if (type == ObjectClassGetter?.Invoke(cot, Array.Empty<object>()) as Type && cot is IEnumerable<MBObjectBase> en && en.FirstOrDefault(o => o.Id == id) is { } result)
                {
                    return result;
                }
            }
            return null;
        }
#endif
    }
}