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

using Cheats.Core.Configs;
using Cheats.Core.Interfaces;
using Cheats.Core.Views;
using UnityEngine;
using Zenject;

namespace Cheats.Core.Factories
{
    internal class CheatsViewFactory : IFactory<ICheat,Transform,CheatBaseView>
    {
        private readonly DiContainer _container;
        private readonly CheatsViewConfig _config;

        public CheatsViewFactory(DiContainer container, CheatsViewConfig config)
        {
            _container = container;
            _config = config;
        }

        public CheatBaseView Create(ICheat cheat, Transform parent)
        {
            var prefab = _config.GetByType(cheat.Type);
            var view = _container.InstantiatePrefab(prefab, parent).GetComponent<CheatBaseView>();
            view.BindTo(cheat);
            return view;
        }
    }
}