using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Chickchilinka.Window.Interfaces
{
    public interface IPrefabProvider : IPrioritized
    {
        UniTask<GameObject> GetContentPrefab(string id);
        UniTask<GameObject> GetTemplatePrefab(string id);
    }
}
