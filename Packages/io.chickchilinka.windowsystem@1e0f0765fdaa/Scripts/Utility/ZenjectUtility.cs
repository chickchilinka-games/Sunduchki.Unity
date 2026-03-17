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
using Chickchilinka.Window.Storages;
using Zenject;

namespace Chickchilinka.Window.Utility
{
    public static class ZenjectUtility
    {
        public static void BindWindowContent<TContent>(this DiContainer container, IInstaller installer = default) where TContent : AbstractContent
        {
            BindWindowComponent(container, typeof(TContent), installer);
        }
        public static void BindWindowTemplate<TTemplate>(this DiContainer container, IInstaller installer = default) where TTemplate : AbstractTemplate
        {
            BindWindowComponent(container, typeof(TTemplate), installer);
        }

        private static void BindWindowComponent(DiContainer container, Type componentType, IInstaller installer)
        {
            container.BindInterfacesTo<ComponentResolver>()
                .AsCached()
                .WithArguments(componentType, installer)
                .OnInstantiated<ComponentResolver>(BindResolverToStorage);
        }

        private static void BindResolverToStorage(InjectContext ctx, ComponentResolver resolver)
        {
            var storage = ctx.Container.Resolve<ContainersStorage>();
            
            storage.Add(resolver);
            resolver.Disposed += () => storage.Remove(resolver);
        }
    }
}