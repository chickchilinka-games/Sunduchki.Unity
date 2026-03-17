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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Chickchilinka.WindowSystemEditor.Utility
{
    public static class ReflectionUtility<TType>
    {
        private static readonly Type[] CachedTypes;
        private static readonly Assembly[] _loadedAssemblies;

        static ReflectionUtility()
        {
            _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            CachedTypes = FindDerivedTypes(_loadedAssemblies, typeof(TType)).ToArray();
        }
        
        public static Type[] GetAllTypes()
        {
            return CachedTypes;
        }
        private static IEnumerable<Type> FindDerivedTypes(Assembly[] assemblies, Type baseType)
        {
            return assemblies.SelectMany(assembly =>
                assembly.GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t)));
        }
    }
}