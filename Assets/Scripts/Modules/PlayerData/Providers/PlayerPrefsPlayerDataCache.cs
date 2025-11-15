using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.PlayerData.Data;
using Modules.PlayerData.Interfaces;
using Newtonsoft.Json;
using UnityEngine;

namespace Modules.PlayerData.Providers
{
    public class PlayerPrefsPlayerDataCache : IPlayerDataCache
    {
        public UniTask Save(PlayerModuleData[] data)
        {
            foreach (var moduleData in data)
            {
                PlayerPrefs.SetString(GetDataKey(moduleData.ModuleName), JsonConvert.SerializeObject(moduleData));
                PlayerPrefs.SetInt(GetVersionKey(moduleData.ModuleName), moduleData.RemoteVersion);
            }
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        public UniTask<IReadOnlyDictionary<string, PlayerModuleData>> Load(string[] keys)
        {
            var resultDictionary = new Dictionary<string, PlayerModuleData>();
            foreach (var key in keys)
            {
                var dataKey = GetDataKey(key);
                if (!PlayerPrefs.HasKey(dataKey)) continue;
                var serializedData = PlayerPrefs.GetString(dataKey);
                var moduleData = JsonConvert.DeserializeObject<PlayerModuleData>(serializedData);
                if (moduleData != null) resultDictionary[key] = moduleData;
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, PlayerModuleData>>(resultDictionary);
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var versionsDictionary = new Dictionary<string, int>();
            foreach (var key in keys)
            {
                var versionKey = GetVersionKey(key);
                if (PlayerPrefs.HasKey(versionKey))
                {
                    versionsDictionary[key] = PlayerPrefs.GetInt(versionKey);
                }
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, int>>(versionsDictionary);
        }

        public UniTask<bool> IsAvailable() => UniTask.FromResult(true);

        private static string GetVersionKey(string key) => $"{key}_RemoteVersion";
        private static string GetDataKey(string key) => $"{key}_PlayerData";
    }
}