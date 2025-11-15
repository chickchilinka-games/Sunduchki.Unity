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

using System;
using System.Collections.Generic;
using ICVR.Window.Basics;
using UnityEngine;
using Zenject;

namespace ICVR.Window.Storages
{
    internal class ContainersStorage : IDisposable
    {
        public event Action<ComponentResolver> ResolverRemoved;
        private readonly Dictionary<Type, ComponentResolver> _componentResolvers = new();
        private readonly DiContainer _defaultContainer;

        public ContainersStorage(DiContainer defaultContainer)
        {
            _defaultContainer = defaultContainer;
        }
        
        public void Dispose()
        {
            _componentResolvers.Clear();
        }

        public void Add(ComponentResolver componentResolver)
        {
            var resolveType = componentResolver.ResolveType;
            if (!_componentResolvers.TryAdd(resolveType, componentResolver))
                Debug.LogError($"Tried to subscribe {resolveType.Name} to multiple containers, which is not allowed:\n" +
                               $"\tSubscribed from {_componentResolvers[resolveType].Installer?.ToString()??"<Unknown>"}\n" +
                               $"\tTried to subscribe from {componentResolver.Installer?.ToString()??"<Unknown>"}.");
        }

        public void Remove(ComponentResolver resolver)
        {
            if (_componentResolvers.TryGetValue(resolver.ResolveType, out var componentResolver) &&
                componentResolver == resolver)
            {
                _componentResolvers.Remove(resolver.ResolveType);
                ResolverRemoved?.Invoke(resolver);
            }
        }
        
        public DiContainer GetContainerFor(Type componentType)
        {
            return _componentResolvers.TryGetValue(componentType, out var resolver) ? resolver.Container : _defaultContainer;
        }
    }
}