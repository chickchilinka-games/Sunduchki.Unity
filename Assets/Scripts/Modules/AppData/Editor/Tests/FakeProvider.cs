using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;

namespace Modules.AppData.Editor.Tests
{
    internal sealed class FakeProvider : IAppDataProvider
    {
        public bool Available = true;
        public readonly Dictionary<string, AppModuleData> RemoteData = new();
        public readonly Dictionary<string, int> RemoteVersions = new();
        
        public UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys)
        {
            var dict = new Dictionary<string, AppModuleData>();
            foreach (var k in keys)
            {
                if (RemoteData.TryGetValue(k, out var v))
                    dict[k] = v;
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, AppModuleData>>(dict);
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            var dict = new Dictionary<string, int>();
            foreach (var k in keys)
            {
                if (RemoteVersions.TryGetValue(k, out var v))
                    dict[k] = v;
            }
            return UniTask.FromResult<IReadOnlyDictionary<string, int>>(dict);
        }

        public UniTask<bool> IsAvailable() => UniTask.FromResult(Available);
    }
}
