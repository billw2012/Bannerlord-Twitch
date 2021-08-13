using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TaleWorlds.CampaignSystem;

namespace BannerlordTwitch.SaveSystem
{
    public class ScopedJsonSync : IDisposable
    {
        private readonly IDataStore dataStore;
        private readonly JsonSerializerSettings settings;
        private readonly MBObjectBaseConverter mboReferences;

        public ScopedJsonSync(IDataStore dataStore, string key)
        {
            this.dataStore = dataStore;
            mboReferences = new MBObjectBaseConverter(dataStore, key);
            settings = new()
            {
                Converters = { new DictionaryToArrayConverter(), new MBGUIDConverter(), mboReferences },
                TypeNameHandling = TypeNameHandling.Auto,
            };
        }

        public bool SyncDataAsJson<T>(string key, ref T data)
        {
            // If the type we're synchronizing is a string or string array, then it's ambiguous
            // with our own internal storage types, which imply that the strings contain valid
            // JSON. Standard binary serialization can handle these types just fine, so we avoid
            // the ambiguity by passing this data straight to the game's binary serializer.
            if (typeof(T) == typeof(string) || typeof(T) == typeof(string[]))
                return dataStore.SyncData(key, ref data);

            if (dataStore.IsSaving)
            {
                string dataJson = JsonConvert.SerializeObject(data, Formatting.None, settings);
                string jsonData = JsonConvert.SerializeObject(new FormatDataPair{ Format = 2, Data = dataJson });
                string[] chunks = ToChunks(jsonData, short.MaxValue - 1024).ToArray();
                return dataStore.SyncData(key, ref chunks);
            }

            if (dataStore.IsLoading)
            {
                try
                {
                    // The game's save system limits the string to be of size of short.MaxValue
                    // We avoid this limitation by splitting the string into chunks.
                    string[] jsonDataChunks = Array.Empty<string>();
                    if (dataStore.SyncData(key, ref jsonDataChunks))
                    {
                        var formatDataPair = JsonConvert.DeserializeObject<FormatDataPair>(
                            ChunksToString(jsonDataChunks ?? Array.Empty<string>()));
                        data = formatDataPair?.Format switch
                        {
                            2 => JsonConvert.DeserializeObject<T>(formatDataPair.Data, settings),
                            _ => data
                        };
                        return true;
                    }
                }
                catch (Exception e) when (e is InvalidCastException) { }

                try
                {
                    // The first version of SyncDataAsJson stored the string as a single entity
                    string jsonData = "";
                    if (dataStore.SyncData(key, ref jsonData)) // try to get as JSON string
                    {
                        data = JsonConvert.DeserializeObject<T>(jsonData, settings);
                        return true;
                    }
                }
                catch (Exception ex) when (ex is InvalidCastException) { }

                try
                {
                    // Most likely the save file stores the data with its default binary serialization
                    // We read it as it is, the next save will convert the data to JSON
                    return dataStore.SyncData(key, ref data);
                }
                catch (Exception ex) when (ex is InvalidCastException) { }
            }

            return false;
        }

        private class FormatDataPair
        {
            public int Format { get; set; }
            public string Data { get; set; }
        }
        
        private static IEnumerable<string> ToChunks(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length-i));
        }

        private static string ChunksToString(string[] chunks)
        {
            if (chunks.Length == 0) return string.Empty;
            if (chunks.Length == 1) return chunks[0];

            var strBuilder = new StringBuilder(short.MaxValue);
            foreach (string chunk in chunks)
                strBuilder.Append(chunk);
            return strBuilder.ToString();
        }

        private void ReleaseUnmanagedResources()
        {
            if (dataStore.IsSaving)
            {
                mboReferences.Save();
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ScopedJsonSync()
        {
            ReleaseUnmanagedResources();
        }
    }
}