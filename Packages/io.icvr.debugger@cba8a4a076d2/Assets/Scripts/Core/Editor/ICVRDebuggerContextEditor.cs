using System;
using System.Collections.Generic;
using System.Linq;
using Core.Installers;
using Core.Interfaces;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Core.Editor
{
    [CustomEditor(typeof(ICVRDebuggerContext))]
    internal class ICVRDebuggerContextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var isNeedUpdate = false;
            EditorGUI.BeginDisabledGroup(true);
            
            var showedPlugins = new Dictionary<string, bool>();
            var context = target as ICVRDebuggerContext;
            var enumerator = context.ScriptableObjectInstallers.GetEnumerator();

            while (enumerator.MoveNext() && enumerator.Current)
            {
                var isInternal = enumerator.Current as DebuggerInternalPluginInstaller;
                
                showedPlugins.Add(enumerator.Current.name, isInternal);
            }

            var orderedShowedPlugins = showedPlugins.OrderBy(pair => !pair.Value);
            
            GUILayout.BeginVertical();

            var externalLabelStyle = new GUIStyle();
            externalLabelStyle.normal.textColor = Color.cyan;

            var pluginsEnumerator = orderedShowedPlugins.GetEnumerator();

            while (pluginsEnumerator.MoveNext() && !string.IsNullOrEmpty(pluginsEnumerator.Current.Key))
            {
                GUILayout.Label(pluginsEnumerator.Current.Key, !pluginsEnumerator.Current.Value ? externalLabelStyle : GUI.skin.label);   
            }

            GUILayout.EndVertical();
            
            // EditorGUILayout.PropertyField(copy, new GUIContent("Plugins"), true);
            EditorGUI.EndDisabledGroup();
            var icvrDebuggerContext = (ICVRDebuggerContext)target;

            if (GUILayout.Button("Search plugins"))
            {
                var mainInstaller = AssetsFinder.GetAllInstances<DebuggerInstaller>();
                var plugins = AssetsFinder.GetAllInstances<DebuggerPluginInstaller>();
                icvrDebuggerContext.ClearMainInstaller();
                icvrDebuggerContext.ClearPlugins();
                icvrDebuggerContext.AddMainInstaller(mainInstaller.First());
                icvrDebuggerContext.AddPlugins(plugins);

                isNeedUpdate = true;
            }

            if (GUILayout.Button("Clear plugins"))
            {
                icvrDebuggerContext.ClearPlugins();

                isNeedUpdate = true;
            }

            GUILayout.Space(20);
            
            serializedObject.ApplyModifiedProperties();

            var prefab = serializedObject.targetObject as ICVRDebuggerContext;

            if (isNeedUpdate)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();   
            }
        }
    }
}
#endif