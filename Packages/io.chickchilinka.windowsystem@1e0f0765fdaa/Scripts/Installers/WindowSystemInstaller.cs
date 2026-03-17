

using Chickchilinka.Window.Data;
using Chickchilinka.Window.Models;
using Chickchilinka.Window.PrefabProviders;
using Chickchilinka.Window.Rules;
using Chickchilinka.Window.Services;
using Chickchilinka.Window.Spawners;
using Chickchilinka.Window.Storages;
using UnityEngine;
using Zenject;

namespace Chickchilinka.Window.Installers
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