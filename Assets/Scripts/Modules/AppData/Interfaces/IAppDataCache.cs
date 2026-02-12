using Cysharp.Threading.Tasks;
using Modules.AppData.Data;

namespace Modules.AppData.Interfaces
{
    public interface IAppDataCache: IAppDataProvider
    {
        UniTask Save(AppModuleData data);
    }
}
