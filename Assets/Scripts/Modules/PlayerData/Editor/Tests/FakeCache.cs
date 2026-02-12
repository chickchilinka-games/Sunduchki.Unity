using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.PlayerData.Data;
using Modules.PlayerData.Interfaces;

namespace Modules.PlayerData.Editor.Tests
{
    internal sealed class FakeCache : IPlayerDataCache
    {
        public readonly Dictionary<string, PlayerModuleData> Local = new();
        public readonly List<PlayerModuleData[]> Saved = new();

        public UniTask<IReadOnlyDictionary<string, PlayerModuleData>> Load(string[] keys)
        {
            var d = new Dictionary<string, PlayerModuleData>();
            foreach (var k in keys)
                if (Local.TryGetValue(k, out var v))
                    d[k] = v;
            return UniTask.FromResult((IReadOnlyDictionary<string, PlayerModuleData>)d);
        }

        public UniTask Save(PlayerModuleData[] data)
        {
            Saved.Add(data);
            foreach (var d in data)
                Local[d.ModuleName] = d;
            return UniTask.CompletedTask;
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var d = new Dictionary<string, int>();
            foreach (var k in keys)
                if (Local.TryGetValue(k, out var v))
                    d[k] = v.RemoteVersion;
            return UniTask.FromResult((IReadOnlyDictionary<string, int>)d);
        }

        public UniTask<bool> IsAvailable() => UniTask.FromResult(true);
    }
}
