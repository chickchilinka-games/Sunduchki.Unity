using Core.Installers;
using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Factories;
using DebuggerPlugins.Logger.LogSenders;
using DebuggerPlugins.Logger.Managers;
using DebuggerPlugins.Logger.Rules;
using DebuggerPlugins.Logger.Services;
using DebuggerPlugins.Logger.Storages;
using DebuggerPlugins.Logger.View;
using UnityEngine;
using Zenject;

namespace DebuggerPlugins.Logger.Installers
{
    [CreateAssetMenu(menuName = "Debugger Plugins/Logger Installer")]
    internal class LoggerInstaller : DebuggerInternalPluginInstaller
    {
        [SerializeField] private LoggerViewConfig loggerViewConfig;

        public override bool IsActive => true;

        public override void InstallBindings()
        {
            BindConfigs();
            BindFactories();
            BindStorages();
            BindServices();
            BindManagers();
            BindViews();
            BindEntities();
        }

        private void BindConfigs()
        {
            Container.Bind<LoggerViewConfig>().FromInstance(loggerViewConfig).AsSingle();
        }

        private void BindFactories()
        {
            Container.BindFactory<LoggerMessage, MessageView, MessageViewFactory>()
                .FromMonoPoolableMemoryPool(x => x
                    .FromComponentInNewPrefab(loggerViewConfig.messageViewPrefab)
                    .UnderTransformGroup("DebuggerLoggerMessageViewPool"));
        }

        private void BindStorages()
        {
            Container.Bind<LoggerMessagesStorage>().AsSingle();
            Container.Bind<LoggerCategoriesStorage>().AsSingle();
        }

        private void BindServices()
        {
            Container.Bind<LoggerMessageService>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoggerService>().AsSingle();
        }

        private void BindManagers()
        {
            Container.Bind<SaveDataManager>().AsSingle();
        }

        private void BindViews()
        {
            Container.BindInterfacesTo<LoggerWindowView>()
                .FromComponentInNewPrefab(loggerViewConfig.loggerWindowViewPrefab).AsSingle();
        }

        private void BindEntities()
        {
            Container.BindInterfacesTo<UnityLogSender>().AsSingle();
            Container.BindInterfacesTo<LoggerPlugin>().AsSingle();
        }
    }
}