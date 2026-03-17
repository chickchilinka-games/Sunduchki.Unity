/*
    Chickchilinka CONFIDENTIAL
    __________________

    [2016] - [2022] Chickchilinka LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of Chickchilinka LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to Chickchilinka LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from Chickchilinka LLC.
*/

using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Chickchilinka.Tools
{
    // ReSharper disable once CheckNamespace
    public class AppEnvironment
    {
        public static readonly AppEnvironment Prod = CreateEnvironment("PROD");
        public static readonly AppEnvironment Dev = CreateEnvironment("DEV");
        public static readonly AppEnvironment Test = CreateEnvironment("TEST");

        public static readonly AppEnvironment Default = Test;

        private static Dictionary<string, AppEnvironment> _environments;

        private static AppEnvironment _current;

        private AppEnvironment(string id)
        {
            Id = id;
        }

        public static AppEnvironment Current
        {
            get
            {
#if CICD_CONFIG
                return _current ??= FromId(AppBuildConfig.Instance.EnvironmentId);
#else
                return Default;
#endif
            }
        }

        public string Id { get; }
        
        public static AppEnvironment FromId(string id)
        {
            id = id.ToUpperInvariant();
            _environments.TryGetValue(id, out var env);
            return env;
        }

        public override string ToString()
        {
            return $"[Environment.{Id}]";
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static AppEnvironment CreateEnvironment(string id)
        {
            // Debug.Log($"[AppEnvironment] CreateEnvironment {id}");
            id = id.ToUpperInvariant();
            var env = new AppEnvironment(id);
            if (_environments == null) _environments = new Dictionary<string, AppEnvironment>();
            if (!_environments.ContainsKey(id))
            {
                _environments.Add(id, env);
            }
            return env;
        }

        public static AppEnvironment[] GetAllEnvironments()
        {
            return _environments.Values.ToArray();
        }

        #if UNITY_EDITOR
        public static void ResetEnvironment(AppEnvironment environment)
        {
            _current = environment;
        }
        #endif
    }
    
    // ReSharper disable once InconsistentNaming
    public interface ICICustomEnvironments
    {
        void Initialize();
    }
}