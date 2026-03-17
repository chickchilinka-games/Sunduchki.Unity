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

using System;
using System.Linq;
using Chickchilinka.Tools.Vault.Entities;
using Chickchilinka.Tools.Vault.Services;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Chickchilinka.Tools
{
    public static class EnvironmentUtil
    {
        // ReSharper disable once UnusedMember.Global
        public static void SelectEnvironmentFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            var prefix = "-env:";
            foreach (var arg in args)
                if (arg.StartsWith(prefix))
                {
                    var env = arg.Replace(prefix, "");
                    SelectEnvironmentFromId(env);
                    return;
                }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void SelectEnvironmentFromId(string id)
        {
            var env = AppEnvironment.FromId(id);
            if (env == null)
            {
                Debug.LogError($"Can't find environment '{id}'");
                return;
            }

            SelectEnvironment(env);
        }

        public static void SelectEnvironment(AppEnvironment env, 
                                            Action successCallback = null, 
                                            Action failCallback = null, 
                                            bool force = false)
        {
            if (AppEnvironment.Current == env && !force && IsEnvironmentSelected(env))
            {
                // just skip it
                return;
            }

            Debug.Log($"[EnvironmentUtil] selected environment: {env}");

            var defines = AppEnvironment.GetAllEnvironments().Select(x => x.Id).ToArray();

            CIUtils.OverrideDefinesForAllTargets(env.Id, defines);

            CIBootstrap.SaveEnvironment(env);
            ApplyEnvironment(env, successCallback, failCallback);
        }
        
        public static AppEnvironment GetSelectedEnvironment()
        {
            return AppEnvironment.Current ?? AppEnvironment.Default;
        }

        private static bool IsDefinePresent(BuildTargetGroup group, string define)
        {
            var defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = defineStr.Split(';');
            return defines.Any(x => string.Compare(x, define, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        private static void ApplyEnvironment(AppEnvironment env, Action successCallback, Action failCallback)
        {
            var ignoreVault = Environment.GetEnvironmentVariable("CI_IGNORE_VAULT") ?? "true";

            if (ignoreVault.Equals("true") || true)
            {
                CIUtils.GetCustomConfig()?.ApplyEnvironment(env, null);
                
                successCallback?.Invoke();
                
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            else
            {
                var projectName = Environment.GetEnvironmentVariable("CI_VAULT_PROJECT") ?? Application.productName;
                var clientName = Environment.GetEnvironmentVariable("CI_VAULT_CLIENT") ?? Application.companyName;
            
                ConfigUtils.TryConnectConfigProvider(projectName, clientName, env.Id, OnConnect);

                void OnConnect(IConfigProvider provider)
                {
                    Debug.Log($"[CI/CD] OnConnect vault provider. Project: {projectName.ToLower()}, Client: {clientName.ToLower()}");
                
                    CIUtils.GetCustomConfig()?.ApplyEnvironment(env, provider);

                    if (provider != null)
                    {
                        successCallback?.Invoke();
                    }
                    else
                    {
                        failCallback?.Invoke();
                    }
                
                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                }
            }
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public static AppEnvironment GetSelectedFromDefines()
        {
            var allEnvs = AppEnvironment.GetAllEnvironments();
            var selected = AppEnvironment.Current;
            foreach (var env in allEnvs)
                if (IsEnvironmentSelected(env))
                    selected = env;
            return selected;
        }

        private static bool IsEnvironmentSelected(AppEnvironment env)
        {
            return IsDefinePresent(EditorUserBuildSettings.selectedBuildTargetGroup, env.Id);
        }
    }
}