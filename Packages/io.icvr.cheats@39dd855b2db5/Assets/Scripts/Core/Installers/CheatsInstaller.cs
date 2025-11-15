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
using Cheats.Core.Factories;
using Cheats.Core.Interfaces;
using Cheats.Core.Services;
using Cheats.Core.Storages;
using Cheats.Core.Views;
using Core.Installers;
using UnityEngine;

namespace Cheats.Core.Installers
{
    [CreateAssetMenu(fileName = "CheatsInstaller", menuName = "Debugger Plugins/Cheats Installer")]
    internal class CheatsInstaller : DebuggerPluginInstaller
    {
        [SerializeField] private CheatsViewConfig _cheatsViewConfig;
        [SerializeField] private bool _isActive;

        public override bool IsActive => _isActive;
        
        public override void InstallBindings()
        {
            BindConfigs();
            BindStorages();
            BindViews();
            BindFactories();
            BindServices();
            Container.BindInterfacesTo<CheatsPlugin>().AsSingle();
        }
        
        private void BindConfigs()
        {
            Container.Bind<CheatsViewConfig>().FromInstance(_cheatsViewConfig).AsSingle();
        }
        
        private void BindViews()
        {
            Container.BindInterfacesTo<CheatsWindowView>()
                .FromComponentInNewPrefab(_cheatsViewConfig.CheatsWindowViewPrefab).AsSingle();
        }

        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<CheatsService>().AsSingle().NonLazy();
        }

        private void BindStorages()
        {
            Container.Bind<CheatsModelStorage>().AsSingle();
        }

        private void BindFactories()
        {
            Container.Bind<CheatsViewFactory>().AsSingle();
        }
    }
}