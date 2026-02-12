using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;

namespace Modules.AppData.Interfaces
{
    public interface IAppDataProvider
    {
        UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys);
        UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys);
        UniTask<bool> IsAvailable();
    }
}
