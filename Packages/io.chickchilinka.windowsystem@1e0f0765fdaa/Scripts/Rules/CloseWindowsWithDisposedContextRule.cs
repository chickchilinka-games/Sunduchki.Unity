// Chickchilinka CONFIDENTIAL
// __________________
// 
// [2016] -  [2024] Chickchilinka LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of Chickchilinka LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to Chickchilinka LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from Chickchilinka LLC.

using System;
using Chickchilinka.Window.Abstract;
using Chickchilinka.Window.Basics;
using Chickchilinka.Window.Services;
using Chickchilinka.Window.Storages;
using Zenject;

namespace Chickchilinka.Window.Rules
{
    internal class CloseWindowsWithDisposedContextRule : IInitializable, IDisposable
    {
        private readonly WindowService _windowService;
        private readonly ContainersStorage _containersStorage;
        private readonly Type _contentType = typeof(AbstractContent);

        public CloseWindowsWithDisposedContextRule(WindowService windowService, ContainersStorage containersStorage)
        {
            _containersStorage = containersStorage;
            _windowService = windowService;
        }

        public void Initialize()
        {
            _containersStorage.ResolverRemoved += WindowResolverRemoved;
        }

        private void WindowResolverRemoved(ComponentResolver resolver)
        {
            if (_contentType.IsAssignableFrom(resolver.ResolveType)) 
                _windowService.CloseWindowsAsync(window => window.Content.GetType() == resolver.ResolveType);
        }

        public void Dispose()
        {
            _containersStorage.ResolverRemoved -= WindowResolverRemoved;
        }
    }
}