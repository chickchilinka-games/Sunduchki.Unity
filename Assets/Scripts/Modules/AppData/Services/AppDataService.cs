using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;
using UnityEngine;

namespace Modules.AppData.Services
{
    public class AppDataService
    {
        private readonly IAppDataProvider _provider;
        private readonly IAppDataCache _cache;
        private readonly IAppDataConsumersCollector _collector;
        private readonly ISerializer _serializer;

        internal AppDataService(
            IAppDataProvider provider,
            IAppDataCache cache,
            IAppDataConsumersCollector collector,
            ISerializer serializer)
        {
            _provider = provider;
            _cache = cache;
            _collector = collector;
            _serializer = serializer;
        }

        public async UniTask<(bool Success, string Error)> TryLoad()
        {
            var consumers = _collector.CollectImplementations().ToArray();
            var keys = consumers.Select(c => c.ModuleName).ToArray();

            try
            {
                if (await _provider.IsAvailable())
                    return await LoadUsingProvider(consumers, keys);

                return await LoadFromCache(consumers, keys);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async UniTask<(bool, string)> LoadUsingProvider(IAppDataConsumer[] consumers, string[] keys)
        {
            var remoteVersions = await _provider.GetVersions(keys);
            var localVersions = await _cache.GetVersions(keys);

            Debug.Log("[AppData] Version comparison:");
            foreach (var key in keys)
            {
                var remoteVersion = remoteVersions.GetValueOrDefault(key, -1);
                var localVersion = localVersions.GetValueOrDefault(key, -1);
                Debug.Log($"[AppData]   {key}: Remote={remoteVersion}, Local={localVersion}");
            }

            var needUpdate = keys
                .Where(k => remoteVersions.TryGetValue(k, out var rv) &&
                            (!localVersions.TryGetValue(k, out var lv) || rv > lv))
                .ToArray();

            Debug.Log(needUpdate.Length > 0
                ? $"[AppData] Modules need update: {string.Join(", ", needUpdate)}"
                : "[AppData] All modules are up to date");

            var finalData = new Dictionary<string, AppModuleData>();

            if (needUpdate.Length > 0)
            {
                var updatedData = await _provider.Load(needUpdate);
                foreach (var entry in updatedData)
                {
                    await _cache.Save(entry.Value);
                    finalData[entry.Key] = entry.Value;
                }
                Debug.Log($"[AppData] Loaded {updatedData.Count} modules from remote");
            }

            var cachedKeys = keys.Except(needUpdate).ToArray();
            if (cachedKeys.Length > 0)
            {
                var cachedData = await _cache.Load(cachedKeys);
                foreach (var entry in cachedData)
                    finalData[entry.Key] = entry.Value;
                Debug.Log($"[AppData] Loaded {cachedData.Count} modules from cache: {string.Join(", ", cachedKeys)}");
            }

            ApplyToConsumers(consumers, finalData);
            return (true, null);
        }

        private async UniTask<(bool, string)> LoadFromCache(IAppDataConsumer[] consumers, string[] keys)
        {
            Debug.Log("[AppData] Loading from cache only (offline mode)");
            var localVersions = await _cache.GetVersions(keys);

            Debug.Log("[AppData] Cached versions:");
            foreach (var key in keys)
            {
                var localVersion = localVersions.GetValueOrDefault(key, -1);
                Debug.Log($"[AppData]   {key}: Local={localVersion}");
            }

            if (keys.Any(k => !localVersions.ContainsKey(k)))
            {
                Debug.LogWarning("[AppData] Version cache is incomplete");
                return (false, "Version cache is incomplete");
            }

            var cachedData = await _cache.Load(keys);

            if (keys.Any(k => !cachedData.ContainsKey(k)))
            {
                Debug.LogWarning("[AppData] Data cache is incomplete");
                return (false, "Data cache is incomplete");
            }

            Debug.Log($"[AppData] Loaded {cachedData.Count} modules from cache");
            ApplyToConsumers(consumers, cachedData);
            return (true, null);
        }

        private void ApplyToConsumers(IEnumerable<IAppDataConsumer> consumers,
            IReadOnlyDictionary<string, AppModuleData> data)
        {
            foreach (var consumer in consumers)
                if (data.TryGetValue(consumer.ModuleName, out var moduleData))
                    consumer.SetData(moduleData.Payload, _serializer);
        }
    }
}
