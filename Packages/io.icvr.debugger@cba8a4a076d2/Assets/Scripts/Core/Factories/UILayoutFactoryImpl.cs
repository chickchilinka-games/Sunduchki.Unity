// ICVR CONFIDENTIAL
// __________________
// 
// [2016] -  [2024] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to ICVR LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using System.Collections.Generic;
using Core.Components;
using Core.Data;
using Core.Interfaces;
using Zenject;

namespace Core.Factories
{
    internal class UILayoutFactoryImpl : IFactory<List<ILayout>, ICVRDebuggerWindow>
    {
        private readonly DiContainer _container;
        private readonly ViewTemplateConfig _config;

        public UILayoutFactoryImpl(ViewTemplateConfig config, DiContainer container)
        {
            _container = container;
            _config =  config;
        }

        public ICVRDebuggerWindow Create(List<ILayout> layouts)
        {
            var prefabComponent = _config.DebuggerWindowPrefab.GetComponentInChildren<ICVRDebuggerWindow>();
            if (!prefabComponent)
                throw new System.Exception("Prefab component ICVRDebuggerWindow not found on the prefab.");
            var debuggerWindow = _container
                .InstantiatePrefab(_config.DebuggerWindowPrefab)
                .GetComponentInChildren<ICVRDebuggerWindow>();
            debuggerWindow.Initialize(layouts);
            return debuggerWindow;
        }
    }
}