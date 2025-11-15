using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Modules.AssetSystem.Providers
{
    public interface IAssetProvider
    {
        UniTask<T> Get<T>(string assetId) where T : Object;
        UniTask Unload<T>(T asset) where T : Object;
    }
}