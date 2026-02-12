using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.PlayerData.Data;

namespace Modules.PlayerData.Interfaces
{
    public interface IPlayerDataProvider
    {
        UniTask<IReadOnlyDictionary<string, PlayerModuleData>> Load(string[] keys);
        UniTask Save(PlayerModuleData[] data);
        UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys);
        UniTask<bool> IsAvailable();
    }

}
