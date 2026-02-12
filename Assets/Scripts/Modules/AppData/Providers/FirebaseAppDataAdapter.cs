#if !UNITY_WEBGL
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;
using Newtonsoft.Json;
using UnityEngine;

namespace Modules.AppData.Providers
{
    public class FirebaseAppDataProvider : IAppDataProvider
    {
        private const string DataCollection = "app_data";
        private const string MetaCollection = "app_meta";
        private const string VersionsDocument = "versions";

        private readonly Lazy<FirebaseFirestore> _db = new Lazy<FirebaseFirestore>(() => FirebaseFirestore.DefaultInstance);

        public async UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var res = new Dictionary<string, int>(keys.Length);
            try
            {
                var snap = await _db.Value.Collection(MetaCollection).Document(VersionsDocument).GetSnapshotAsync();
                if (!snap.Exists)
                {
                    throw new InvalidOperationException($"The document {VersionsDocument} does not exist.");
                }

                if (snap.ContainsField("versions"))
                {
                    var map = snap.GetValue<Dictionary<string, int>>("versions");
                    foreach (var k in keys)
                    {
                        if (map != null && map.TryGetValue(k, out var version))
                            res[k] = version;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return res;
        }

        public async UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys)
        {
            var dict = new Dictionary<string, AppModuleData>(keys.Length);

            var versionsSnapshot = await _db.Value.Collection(MetaCollection).Document(VersionsDocument).GetSnapshotAsync();
            var versionMap = new Dictionary<string, int>();
            if (versionsSnapshot.ContainsField("versions"))
            {
                versionMap = versionsSnapshot.GetValue<Dictionary<string, int>>("versions");
            }

            foreach (var key in keys)
            {
                var snapshot = await _db.Value.Collection(DataCollection).Document(key).GetSnapshotAsync();
                if (!snapshot.Exists)
                    throw new KeyNotFoundException($"Key: {key} not found in firestore");

                var payloadMap = snapshot.ToDictionary();
                
                await LoadSubCollections(key, payloadMap);

                var payloadJson = JsonConvert.SerializeObject(payloadMap);

                var version = versionMap.GetValueOrDefault(key, 0);
                dict[key] = new AppModuleData
                {
                    ModuleName = key,
                    Version = version,
                    Payload = payloadJson
                };
            }

            return dict;
        }

        private async UniTask LoadSubCollections(string moduleName, Dictionary<string, object> payloadMap)
        {
            try
            {
                var moduleDoc = _db.Value.Collection(DataCollection).Document(moduleName);
                var metaSnapshot = await _db.Value.Collection(MetaCollection).Document(moduleName).GetSnapshotAsync();

                if (!metaSnapshot.Exists || !metaSnapshot.ContainsField("collections"))
                    return;

                var collectionNames = metaSnapshot.GetValue<string[]>("collections");
                if (collectionNames == null || collectionNames.Length == 0)
                    return;

                foreach (var collectionName in collectionNames)
                {
                    var collectionRef = moduleDoc.Collection(collectionName);
                    var collectionSnapshot = await collectionRef.GetSnapshotAsync();
                    var collectionData = new List<Dictionary<string, object>>();

                    foreach (var doc in collectionSnapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            collectionData.Add(doc.ToDictionary());
                        }
                    }

                    if (collectionData.Count > 0)
                    {
                        payloadMap[collectionName] = collectionData;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseAppDataProvider] Failed to load subcollections for module {moduleName}: {ex.Message}");
            }
        }

        public async UniTask<bool> IsAvailable()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;

            try
            {
                await _db.Value.Collection(MetaCollection).Document(VersionsDocument).GetSnapshotAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
    }
}
#endif
