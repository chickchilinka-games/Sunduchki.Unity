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

using ICVR.Window.Data;
using ICVR.Window.Models;
using ICVR.Window.PrefabProviders;
using ICVR.Window.Rules;
using ICVR.Window.Services;
using ICVR.Window.Spawners;
using ICVR.Window.Storages;
using UnityEngine;
using Zenject;

namespace ICVR.Window.Installers
{
    public class WindowSystemInstaller : Installer
    {
        private readonly WindowSystemConfig _windowsConfig = Resources.Load<WindowSystemConfig>(Consts.Name.ConfigAssetName);

        public override void InstallBindings()
        {
            if (_windowsConfig) 
                Container.BindInstance(_windowsConfig);
            
            Container.Bind<WindowSystem>().AsSingle();
            InstallServices();
            InstallModels();
            InstallFactoriesAndPools();
            InstallProviders();
            InstallStorages();
            InstallRules();
        }

        private void InstallModels()
        {
            Container.BindInterfacesAndSelfTo<WindowSystemModel>().AsSingle();
        }

        private void InstallServices()
        {
            Container.Bind<WindowService>().AsSingle();
        }

        private void InstallProviders()
        {
            Container.BindInterfacesAndSelfTo<ResourcePrefabProvider>().AsSingle();
            if (_windowsConfig && _windowsConfig.AddressablesSupport) 
                Container.BindInterfacesAndSelfTo<AddressablePrefabProvider>().AsSingle();
        }

        private void InstallFactoriesAndPools()
        {
            Container.BindInterfacesAndSelfTo<WindowFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<WindowContentPool>().AsSingle();
            Container.BindInterfacesAndSelfTo<WindowTemplatePool>().AsSingle();
        }

        private void InstallStorages()
        {
            Container.BindInterfacesAndSelfTo<ContainersStorage>().AsSingle();
        }

        private void InstallRules()
        {
            Container.BindInterfacesTo<LookingForHoldersRule>().AsSingle();
            Container.BindInterfacesTo<CloseWindowsWithDisposedContextRule>().AsSingle();
        }
    }
}