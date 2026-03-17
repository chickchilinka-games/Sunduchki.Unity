

using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Abstract;
using Chickchilinka.Window.Interfaces;
using Chickchilinka.Window.Utility;
using Zenject;
using Zenject.Internal;

namespace Chickchilinka.Window.Spawners
{
    internal class WindowContentPool : PrefabSpawner
    {
        
        public async UniTask<IContent> Spawn(string id, object[] args)
        {
            var prefab = await GetContentPrefab(id);
            return TryInstantiateWindowComponent(prefab, args, out AbstractContent content)
                ? content
                : WindowUtility.CreateBrokenContent();
        }

        public bool CanSpawn<TContent>() where TContent : AbstractContent
        {
            var contentType = typeof(TContent);
            var container = ContainerStorage.GetContainerFor(contentType);
            var typeInfo = TypeAnalyzer.TryGetInfo(contentType);
            foreach (var info in typeInfo.AllInjectables)
            {
                if (info.Optional)
                    continue;
                if (info.Identifier == null)
                {
                    if (!container.HasBinding(info.MemberType))
                        return false;
                }
                else
                {
                    if (!container.HasBindingId(info.MemberType, info.Identifier))
                        return false;
                }
                
            }
            return true;
        }
    }
}