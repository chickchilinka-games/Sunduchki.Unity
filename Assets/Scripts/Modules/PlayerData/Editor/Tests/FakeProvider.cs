using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.PlayerData.Data;
using Modules.PlayerData.Interfaces;

namespace Modules.PlayerData.Editor.Tests
{
    internal sealed class FakeProvider : IPlayerDataProvider
    {
        public bool Available = true;
        public readonly Dictionary<string, PlayerModuleData> RemoteData = new();
        public readonly Dictionary<string, int> RemoteVersions = new();
        public readonly List<PlayerModuleData[]> Saved = new();
        public readonly List<string[]> LoadCalls = new();

        public UniTask<IReadOnlyDictionary<string, PlayerModuleData>> Load(string[] keys)
        {
            LoadCalls.Add(keys);
            var d = new Dictionary<string, PlayerModuleData>();
            foreach (var k in keys)
                if (RemoteData.TryGetValue(k, out var v))
                    d[k] = v;
            return UniTask.FromResult<IReadOnlyDictionary<string, PlayerModuleData>>(d);
        }

        public UniTask Save(PlayerModuleData[] data)
        {
            Saved.Add(data);
            foreach (var d in data)
            {
                RemoteVersions[d.ModuleName] = d.RemoteVersion;
                RemoteData[d.ModuleName] = d;
            }
            return UniTask.CompletedTask;
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var d = new Dictionary<string, int>();
            foreach (var k in keys)
                if (RemoteVersions.TryGetValue(k, out var v))
                    d[k] = v;
            return UniTask.FromResult<IReadOnlyDictionary<string, int>>(d);
        }

        public UniTask<bool> IsAvailable() => UniTask.FromResult(Available);
    }
}