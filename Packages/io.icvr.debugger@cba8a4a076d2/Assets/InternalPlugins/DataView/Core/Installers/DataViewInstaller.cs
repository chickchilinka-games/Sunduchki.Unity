using System.Globalization;
using Core.Extensions;
using Core.Installers;
using DebuggerPlugins.DataView.Core.Data;
using DebuggerPlugins.DataView.Core.Factories;
using DebuggerPlugins.DataView.Core.Interfaces;
using DebuggerPlugins.DataView.Core.Resolvers;
using DebuggerPlugins.DataView.Core.Services;
using DebuggerPlugins.DataView.Core.Views;
using DebuggerPlugins.TestDataView.Core.Data;
#if CICD_CONFIG
using ICVR.Tools;
#endif
using InternalPlugins.DataView.Core.Data.ImplementsData;
using InternalPlugins.DataView.Core.Models;
using InternalPlugins.DataView.Core.Rules;
using InternalPlugins.DataView.Core.Storages;
using UnityEngine;

namespace InternalPlugins.DataView.Core.Installers
{
    [CreateAssetMenu(menuName = "Debugger Plugins/DataView Installer")]
    internal class DataViewInstaller : DebuggerInternalPluginInstaller
    {
        [SerializeField] private DataViewConfig _dataViewConfig;
        [SerializeField] private bool _isActive;

        private const string CompanyNameTitle = "CompanyName";
        private const string BuildVersionTitle = "BuildVersion";
        private const string GraphicsLevelSwitcher = "Graphics level switcher";
        private const string RAMTitle = "RAM";
        private const string CPUTitle = "CPU";
        private const string DeviceType = "DeviceType";
        private const string DeviceTitle = "DeviceName";
        private const string OSTitle = "OS";
        private const string DisplayDPITitle = "Display DPI";
        private const string ResolutionsTitle = "Resolutions";
        private const string PlatformTitle = "Platform";
        private const string EnvironmentTitle = "Environment";
        private const string DebugModeStatusTitle = "DebugModeStatus";

        public override bool IsActive => _isActive;

        public override void InstallBindings()
        {
            BindConfigs();
            BindResolvers();
            BindStorages();
            BindViews();
            BindRules();
            BindFactories();
            BindData();
            BindStaticData();
            BindServices();
        }

        private void BindFactories()
        {
            Container.BindFactory<IStringData, StringDataView, StringDataFactory>().FromComponentInNewPrefab(_dataViewConfig.StringDataViewPrefab);
        }

        private void BindRules()
        {
            Container.BindRule<ResolveRule>();
        }

        private void BindConfigs()
        {
            Container.Bind<DataViewConfig>().FromInstance(_dataViewConfig).AsSingle();
        }

        private void BindViews()
        {
            Container.BindInterfacesTo<DataWindowView>()
                .FromComponentInNewPrefab(_dataViewConfig.DataWindowViewPrefab).AsSingle();
        }

        private void BindServices()
        {
            Container.BindInterfacesTo<DataViewPlugin>().AsSingle();
            Container.Bind<DataViewService>().AsSingle();
        }

        private void BindStorages()
        {
            Container.Bind<LayoutsStorage>().AsSingle();
        }

        private void BindResolvers()
        {
            Container.Bind<DataResolver>().AsSingle();
            Container.Bind<LayoutResolver>().AsSingle();
        }

        private void BindData()
        {
            Container.BindInterfacesTo<BatteryStatusData>().AsSingle();
            Container.BindInterfacesTo<BatteryLevelData>().AsSingle();
            Container.BindInterfacesTo<FpsData>().AsSingle();
            Container.BindInterfacesTo<OrientationData>().AsSingle();
            Container.BindInterfacesTo<IsFullScreenData>().AsSingle();
        }

        private void BindStaticData()
        {
            AddData(CompanyNameTitle,  Application.companyName);
#if CICD_CONFIG
            AddData(BuildVersionTitle, AppVersion.Current.ToString());
            AddData(EnvironmentTitle, AppEnvironment.Current.Id);      
#else
            AddData(BuildVersionTitle, Application.version);
            AddData(EnvironmentTitle, "DEV");
#endif
            AddData(DebugModeStatusTitle, Debug.isDebugBuild.ToString());
            AddData(GraphicsLevelSwitcher, QualitySettings.GetQualityLevel().ToString());
            AddData(RAMTitle, $"{SystemInfo.systemMemorySize} Mb");
            AddData(CPUTitle, SystemInfo.processorType);
            AddData(DeviceType, SystemInfo.deviceType.ToString());
            AddData(DeviceTitle, SystemInfo.deviceName);
            AddData(OSTitle, SystemInfo.operatingSystem);
            AddData(DisplayDPITitle, Screen.dpi.ToString(CultureInfo.InvariantCulture));
            var currentResolution = Screen.currentResolution;
            AddData(ResolutionsTitle, $"{currentResolution.width}x{currentResolution.height} refreshRate {currentResolution.refreshRate} Hz");
            AddData(PlatformTitle, Application.platform.ToString());
        }

        private void AddData(string title, string value)
        {
            Container.BindInterfacesTo<StringData>().FromInstance(new StringData(title, value));
        }
    }
}