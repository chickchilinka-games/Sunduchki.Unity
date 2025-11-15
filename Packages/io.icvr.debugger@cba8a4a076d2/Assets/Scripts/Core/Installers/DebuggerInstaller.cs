using System.Collections.Generic;
using Core.Components;
using Core.Data;
using Core.Extensions;
using Core.Factories;
using Core.Interfaces;
using Core.Model;
using Core.Rules;
using Core.Services;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    [CreateAssetMenu(fileName = "DebuggerInstaller", menuName = "ICVR Debugger/DebuggerInstaller")]
    internal class DebuggerInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private bool _alwaysAvailable = true;
        [SerializeField] private OpenTriggerData _openTriggerData;
        [SerializeField] private TabConfig tabConfig;
        [SerializeField] private ViewTemplateConfig _viewTemplateConfig;
        public override void InstallBindings()
        {
            Container.BindInstance(_viewTemplateConfig).AsSingle();
            // Bind TabConfig as a single instance
            Container.Bind<TabConfig>()
                .FromScriptableObject(tabConfig)
                .AsSingle();

            Container.BindFactory<PageTab, TabFactory>()
                .FromMethod(container => container.InstantiatePrefabForComponent<PageTab>(tabConfig.TabPrefab));
            
            Container.Bind<ICVRDebugger>().AsSingle();
            
            Container.Bind<PluginResolver>().AsSingle();
            Container.Bind<PluginService>().AsSingle();
            Container.Bind<LayoutResolver>().AsSingle();
            Container.Bind<LayoutService>().AsSingle();
            Container.Bind<InternalDebuggerService>().AsSingle();

            Container.BindIFactory<List<ILayout>, ICVRDebuggerWindow>().FromFactory<UILayoutFactoryImpl>();
            Container.Bind<ICVRDebuggerModel>().AsSingle();
            Container.Bind<LayoutManager>().AsSingle();

            Container.BindRule<InitializeRule>();
            Container.BindRule<EventSystemStabilityRule>();
            
            if (_alwaysAvailable)
            {

#if ENABLE_INPUT_SYSTEM
                Container.BindRule<NewInputOpenTriggerRule>(_openTriggerData);
#else
                Container.BindRule<LegacyInputManagerOpenTriggerRule>(_openTriggerData); 
#endif
            }
        }
    }
}