using System;
using System.Collections.Generic;
using System.Reflection;

namespace ICVR.Tools.Vault.Utils
{
    internal class ReflectionUtils
    {
        public static IEnumerable<Type> GetImplementationsFromCurrentAssembly<TInterface>()
        {
            if (!typeof(TInterface).IsInterface) return Type.EmptyTypes;

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            var implementations = new List<Type>();

            foreach (var type in types)
            {
                if (type.IsInterface || type.IsAbstract) continue;

                var isImplemented = type.GetInterface($"{typeof(TInterface)}") != null;

                if (!isImplemented) continue;

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