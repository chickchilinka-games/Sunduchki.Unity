

using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Abstract;
using Chickchilinka.Window.Interfaces;
using Chickchilinka.Window.Utility;

namespace Chickchilinka.Window.Spawners
{
    internal class WindowTemplatePool : PrefabSpawner
    {
        public async UniTask<ITemplate> Spawn(string id, object[] args)
        {
            var prefab = await GetTemplatePrefab(id);
            return TryInstantiateWindowComponent(prefab, args, out AbstractTemplate template)
                ? template
                : WindowUtility.CreateBrokenTemplate();
        }
    }
}