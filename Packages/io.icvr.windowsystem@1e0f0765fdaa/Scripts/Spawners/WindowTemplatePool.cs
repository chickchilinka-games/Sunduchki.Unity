

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Interfaces;
using ICVR.Window.Utility;

namespace ICVR.Window.Spawners
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