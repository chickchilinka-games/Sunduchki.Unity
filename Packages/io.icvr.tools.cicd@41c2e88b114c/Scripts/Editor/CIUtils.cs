/*
    ICVR CONFIDENTIAL
    __________________

    [2016] - [2022] ICVR LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of ICVR LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to ICVR LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from ICVR LLC.
*/

using System;
using System.Linq;
using System.Reflection;
using ICVR.Tools.Vault.Services;
using JetBrains.Annotations;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace ICVR.Tools
{
    // ReSharper disable once InconsistentNaming
    public interface ICICustomConfig
    {
        void ApplyCustomSetup();
        string[] GetSceneListForTarget(BuildTarget buildTarget);    
        void PostProcessBuild();
        void ApplyEnvironment(AppEnvironment environment, [CanBeNull] IConfigProvider configProvider);
    }
    
    // ReSharper disable once InconsistentNaming
    internal static class CIUtils
    {
        private static ICICustomConfig _customConfig;
        private static ICICustomEnvironments _customEnvironments;
        public static readonly string[] DefaultMask = {""};

        public static ICICustomConfig GetCustomConfig()
        {
            if (_customConfig != null) return _customConfig;

            var config = GetCustomConfigType();
            if (config == null) return null;

            _customConfig = Activator.CreateInstance(config) as ICICustomConfig;
            return _customConfig;
        }

        private static Type GetCustomConfigType()
        {
            try
            {
                var editorAssembly = Assembly.Load("Assembly-CSharp-Editor-firstpass");
                var configType = typeof(ICICustomConfig);
                return editorAssembly
                    .GetTypes()
                    .FirstOrDefault(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(configType)));
            }
            catch
            {
                return null;
            }
        }
        
        public static ICICustomEnvironments GetCustomEnvironments()
        {
            if (_customEnvironments != null) return _customEnvironments;

            var config = GetCustomEnvironmentsType();
            if (config == null) return null;

            _customEnvironments = Activator.CreateInstance(config) as ICICustomEnvironments;
            return _customEnvironments;
        }

        private static Type GetCustomEnvironmentsType()
        {
            try
            {
                var editorAssembly = Assembly.Load("Assembly-CSharp-firstpass");
                var environmentsType = typeof(ICICustomEnvironments);
                return editorAssembly
                    .GetTypes()
                    .FirstOrDefault(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(environmentsType)));
            }
            catch
            {
                return null;
            }
        }

        public static void OverrideDefines(BuildTargetGroup group, string define, string[] maskDefines)
        {
            // scripting defines
            var defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = defineStr.Split(';');

            //Debug.Log($"[EnvironmentUtil] group: {group}, current defines: {defineStr}");

            var definesWithoutEnvironment =
                defines.Where(def =>
                    maskDefines.All(x => string.Compare(x, def, StringComparison.InvariantCultureIgnoreCase) != 0));

            defineStr = string.Join(";", definesWithoutEnvironment.Append(define));

            //Debug.Log($"[EnvironmentUtil] group: {group}, new defines: {defineStr}");

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defineStr);
        }

        public static void OverrideDefinesForAllTargets(string define, string[] maskDefines)
        {
            // Debug.Log($"Override defines {define}");
            OverrideDefines(BuildTargetGroup.Android, define, maskDefines);
            OverrideDefines(BuildTargetGroup.iOS, define, maskDefines);
            OverrideDefines(BuildTargetGroup.Standalone, define, maskDefines);
        }
    }
}