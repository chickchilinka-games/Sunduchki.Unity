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


using Cheats.Core.Factories;
using Cheats.Core.Interfaces;
using Cheats.Core.Storages;
using Core.Enums;
using Core.Interfaces;
using R3;
using UnityEngine;
using Zenject;

namespace Cheats.Core.Views
{
    internal class CheatsWindowView : MonoBehaviour, ILayout
    {
        [Header("ILayout fields")]
        [SerializeField] private string id;
        [SerializeField] private LayoutType layoutType;
        [SerializeField] private RectTransform pivot;
        [Space]
        [SerializeField] private RectTransform layoutPanel;
        
        public string Id => id;
        public LayoutType LayoutType => layoutType;
        public RectTransform Pivot => pivot;

        private CheatsModelStorage _cheatsModelStorage;
        private CheatsViewFactory _cheatsViewFactory;

        [Inject]
        internal void Construct(CheatsModelStorage cheatsModelStorage, CheatsViewFactory cheatsViewFactory)
        {
            _cheatsModelStorage = cheatsModelStorage;
            _cheatsViewFactory = cheatsViewFactory;
            
            Initialize();
        }

        private void Initialize()
        {
            _cheatsModelStorage.GetAll().ForEach(CreateView);
            _cheatsModelStorage.ElementAdded.Subscribe(CreateView);
        }

        private void CreateView(ICheat cheat)
        {
            var view = _cheatsViewFactory.Create(cheat, layoutPanel);
            view.Initialize(cheat.Order);
        }
    }
}