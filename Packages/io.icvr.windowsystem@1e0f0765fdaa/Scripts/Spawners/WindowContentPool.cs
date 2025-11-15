// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Interfaces;
using ICVR.Window.Utility;
using Zenject;
using Zenject.Internal;

namespace ICVR.Window.Spawners
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