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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace ICVR.Tools.Vault.Utils
{
    internal class ReflectionUtils
    {
        public static IEnumerable<Type> GetImplementationsFromCurrentAssembly<TInterface>()
        {
            if (!typeof(TInterface).IsInterface)
            {
                return Type.EmptyTypes;
            }
            
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            var implementations = new List<Type>();
            
            foreach (var type in types)
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    continue;
                }

                var isImplemented = type.GetInterface($"{typeof(TInterface)}") != null;

                if (!isImplemented)
                {
                    continue;
                }
                
                implementations.Add(type);
            }

            return implementations;
        }

        public static T CreateInstance<T>(Type type) where T : class
        {
            var instance = (T)Activator.CreateInstance(type);
            
            return instance;
        }
    }
}