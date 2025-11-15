using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Modules.PlayerData.Data;
using Modules.PlayerData.Interfaces;
using UnityEngine;

namespace Modules.PlayerData.Services
{
    public class PlayerDataService
    {
        private readonly IPlayerDataProvider _provider;
        private readonly IPlayerDataCache _cache;
        private readonly IPlayerDataConsumersCollector _collector;
        private readonly ISerializer _serializer;

        public PlayerDataService(
            IPlayerDataProvider provider,
            IPlayerDataCache cache,
            IPlayerDataConsumersCollector collector,
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
                    return await LoadWithRemote(consumers, keys);

                return await LoadFromCacheOnly(consumers, keys);
            }
            catch (Exception exception)
            {
                return (false, exception.Message);
            }
        }

        public async UniTask<(bool Success, string Error)> TrySave()
        {
            var consumers = _collector.CollectImplementations().ToArray();
            var keys = consumers.Select(c => c.ModuleName).ToArray();
            var newData = BuildPayloads(consumers);

            try
            {
                if (await _provider.IsAvailable())
                    return await SaveWithRemote(keys, newData);

                return await SaveOffline(keys, newData);
            }
            catch (Exception exception)
            {
                return (false, exception.Message);
            }
        }

        private Dictionary<string, string> BuildPayloads(IEnumerable<IPlayerDataConsumer> consumers)
            => consumers.ToDictionary(consumer => consumer.ModuleName, consumer => consumer.GetSerializedData(_serializer));

        private async UniTask<(bool, string)> LoadWithRemote(IPlayerDataConsumer[] consumers, string[] keys)
        {
            var remoteVersions = await _provider.GetVersions(keys);
            var cachedModules = await _cache.Load(keys);
            var cacheVersions = await _cache.GetVersions(keys);

            Debug.Log("[PlayerData] Version comparison:");
            foreach (var key in keys)
            {
                var remoteVersion = remoteVersions.GetValueOrDefault(key, -1);
                var cacheVersion = cacheVersions.GetValueOrDefault(key, -1);
                var baseVersion = cachedModules.TryGetValue(key, out var module) ? module.BaseVersion : -1;
                var isPending = cachedModules.TryGetValue(key, out var mod) && mod.IsPendingUpload;
                Debug.Log($"[PlayerData]   {key}: Remote={remoteVersion}, Cache={cacheVersion}, Base={baseVersion}, Pending={isPending}");
            }

            var pendingReadyForUpload = cachedModules
                .Where(moduleDataPair => moduleDataPair.Value.IsPendingUpload
                               && remoteVersions.TryGetValue(moduleDataPair.Key, out var remoteVersion)
                               && moduleDataPair.Value.BaseVersion == remoteVersion)
                .Select(moduleDataPair => moduleDataPair.Key)
                .ToArray();

            if (pendingReadyForUpload.Length > 0)
                Debug.Log($"[PlayerData] Pending uploads ready: {string.Join(", ", pendingReadyForUpload)}");

            if (pendingReadyForUpload.Length > 0)
                await UploadBatch(pendingReadyForUpload, cachedModules);

            var keysToFetchFromRemote = keys.Where(key =>
                    !remoteVersions.TryGetValue(key, out var remoteVersion)
                    || !cacheVersions.TryGetValue(key, out var cacheVersion)
                    || cacheVersion < remoteVersion
                    || (cachedModules.TryGetValue(key, out var cachedEntry) && cachedEntry.IsPendingUpload &&
                        cachedEntry.BaseVersion != remoteVersion))
                .ToArray();

            if (keysToFetchFromRemote.Length > 0)
                Debug.Log($"[PlayerData] Will fetch from remote: {string.Join(", ", keysToFetchFromRemote)}");

            var result = new Dictionary<string, PlayerModuleData>();

            if (keysToFetchFromRemote.Length > 0)
            {
                var fetchedFromRemote = await _provider.Load(keysToFetchFromRemote);
                await _cache.Save(fetchedFromRemote.Values.ToArray());
                Merge(result, fetchedFromRemote);
                Debug.Log($"[PlayerData] Loaded {fetchedFromRemote.Count} modules from remote");

                var missingFromRemote = keysToFetchFromRemote
                    .Where(key => !fetchedFromRemote.ContainsKey(key))
                    .ToArray();

                if (missingFromRemote.Length > 0)
                {
                    var dataFromCache = await _cache.Load(missingFromRemote);
                    Merge(result, dataFromCache);
                }
            }

            var keysFromCacheOnly = keys.Except(keysToFetchFromRemote).ToArray();
            if (keysFromCacheOnly.Length > 0)
            {
                Merge(result, await _cache.Load(keysFromCacheOnly));
                Debug.Log($"[PlayerData] Loaded {keysFromCacheOnly.Length} modules from cache: {string.Join(", ", keysFromCacheOnly)}");
            }

            Apply(consumers, result);
            return (true, null);
        }

        private async UniTask UploadBatch(string[] keys, IReadOnlyDictionary<string, PlayerModuleData> cacheData)
        {
            foreach (var key in keys)
            {
                var moduleData = cacheData[key];
                moduleData.RemoteVersion++;
                moduleData.IsPendingUpload = false;
                await _provider.Save(new[] { moduleData });
                await _cache.Save(new[] { moduleData });
            }
        }

        private static void Merge(
            IDictionary<string, PlayerModuleData> target,
            IReadOnlyDictionary<string, PlayerModuleData> source)
        {
            foreach (var keyValuePair in source) target[keyValuePair.Key] = keyValuePair.Value;
        }

        private void Apply(IEnumerable<IPlayerDataConsumer> consumers,
            IReadOnlyDictionary<string, PlayerModuleData> data)
        {
            foreach (var consumer in consumers)
                if (data.TryGetValue(consumer.ModuleName, out var moduleData))
                    consumer.SetData(moduleData.Payload, _serializer);
        }

        private async UniTask<(bool, string)> LoadFromCacheOnly(IPlayerDataConsumer[] consumers, string[] keys)
        {
            Debug.Log("[PlayerData] Loading from cache only (offline mode)");
            var localVersions = await _cache.GetVersions(keys);

            Debug.Log("[PlayerData] Cached versions:");
            foreach (var key in keys)
            {
                var cacheVersion = localVersions.GetValueOrDefault(key, -1);
                Debug.Log($"[PlayerData]   {key}: Cache={cacheVersion}");
            }

            if (keys.Any(key => !localVersions.ContainsKey(key)))
            {
                Debug.LogWarning("[PlayerData] Provider unavailable and cache incomplete");
                return (false, "Provider unavailable and cache incomplete");
            }

            var cachedData = await _cache.Load(keys);
            Debug.Log($"[PlayerData] Loaded {cachedData.Count} modules from cache");
            Apply(consumers, cachedData);
            return (true, null);
        }

        private async UniTask<(bool, string)> SaveWithRemote(string[] keys, Dictionary<string, string> newPayloads)
        {
            Debug.Log("[PlayerData] Saving with remote available");
            var remoteVersions = await _provider.GetVersions(keys);
            var cachedModules = await _cache.Load(keys);

            Debug.Log("[PlayerData] Pre-save versions:");
            foreach (var key in keys)
            {
                var remoteVersion = remoteVersions.GetValueOrDefault(key, -1);
                var cacheVersion = cachedModules.TryGetValue(key, out var module) ? module.RemoteVersion : -1;
                Debug.Log($"[PlayerData]   {key}: Remote={remoteVersion}, Cache={cacheVersion}");
            }

            var changedKeys = keys
                .Where(key =>
                    !cachedModules.TryGetValue(key, out var cachedEntry) || cachedEntry.Payload != newPayloads[key])
                .ToArray();

            Debug.Log(changedKeys.Length > 0
                ? $"[PlayerData] Modules changed and will be saved: {string.Join(", ", changedKeys)}"
                : "[PlayerData] No modules changed");

            foreach (var key in changedKeys)
            {
                var baseVersion = remoteVersions.GetValueOrDefault(key, 0);
                var entry = cachedModules.TryGetValue(key, out var cachedEntry)
                    ? cachedEntry
                    : new PlayerModuleData { ModuleName = key };

                entry.Payload = newPayloads[key];
                entry.BaseVersion = baseVersion;
                entry.RemoteVersion++;
                entry.IsPendingUpload = false;

                Debug.Log($"[PlayerData] Saved {key}: BaseVersion={entry.BaseVersion}, NewRemoteVersion={entry.RemoteVersion}");
                await _cache.Save(new[] { entry });
                await _provider.Save(new[] { entry });
            }

            return (true, null);
        }

        private async UniTask<(bool, string)> SaveOffline(string[] keys, Dictionary<string, string> newPayloads)
        {
            Debug.Log("[PlayerData] Saving offline (remote unavailable)");
            var cachedModules = await _cache.Load(keys);

            Debug.Log("[PlayerData] Current cached versions:");
            foreach (var key in keys)
            {
                var cacheVersion = cachedModules.TryGetValue(key, out var module) ? module.RemoteVersion : -1;
                var baseVersion = cachedModules.TryGetValue(key, out var mod) ? mod.BaseVersion : -1;
                Debug.Log($"[PlayerData]   {key}: Cache={cacheVersion}, Base={baseVersion}");
            }

            var touchedKeys = keys
                .Where(key =>
                    !cachedModules.TryGetValue(key, out var cachedEntry) || cachedEntry.Payload != newPayloads[key])
                .ToArray();

            Debug.Log(touchedKeys.Length > 0
                ? $"[PlayerData] Modules changed and will be saved offline: {string.Join(", ", touchedKeys)}"
                : "[PlayerData] No modules changed");

            foreach (var key in touchedKeys)
            {
                var entry = cachedModules.TryGetValue(key, out var cachedEntry)
                    ? cachedEntry
                    : new PlayerModuleData { ModuleName = key };

                entry.Payload = newPayloads[key];
                entry.IsPendingUpload = true;
                entry.BaseVersion = entry.RemoteVersion;
                entry.RemoteVersion++;
                Debug.Log($"[PlayerData] Saved offline {key}: BaseVersion={entry.BaseVersion}, NewRemoteVersion={entry.RemoteVersion}, PendingUpload=true");
                await _cache.Save(new[] { entry });
            }

            return (true, null);
        }
    }
}