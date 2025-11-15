using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;
using Newtonsoft.Json;
using UnityEngine;

namespace Modules.AppData.Providers
{
    public class PlayerPrefsAppDataCache : IAppDataCache
    {
        public UniTask Save(AppModuleData data)
        {
            PlayerPrefs.SetString(data.ModuleName, JsonConvert.SerializeObject(data));
            var versionKey = GetVersionKey(data.ModuleName);
            PlayerPrefs.SetInt(versionKey, data.Version);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        public UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys)
        {
            var dictionary = new Dictionary<string, AppModuleData>();
            foreach (var key in keys)
            {
                if(!PlayerPrefs.HasKey(key))
                    continue;
                
                var serializedData = PlayerPrefs.GetString(key);
                var data = JsonConvert.DeserializeObject<AppModuleData>(serializedData);
                dictionary.Add(key, data);
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, AppModuleData>>(dictionary);
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var dictionary = new Dictionary<string, int>();
            foreach (var key in keys)
            {
                var versionKey = GetVersionKey(key);
                if (!PlayerPrefs.HasKey(versionKey))
                {
                    continue;
                }

                var version = PlayerPrefs.GetInt(versionKey);
                dictionary.Add(key, version);
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, int>>(dictionary);
        }

        public UniTask<bool> IsAvailable()
        {
            return UniTask.FromResult(true);
        }

        private string GetVersionKey(string key)
        {
            return $"{key}_Version";
        }
    }
}