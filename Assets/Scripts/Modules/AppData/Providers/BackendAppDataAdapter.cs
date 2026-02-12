using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;

namespace Modules.AppData.Providers
{
    public class BackendAppDataAdapter: IAppDataProvider
    {
        public UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> IsAvailable()
        {
            throw new System.NotImplementedException();
        }
    }
}
