using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Modules.AssetSystem.Editor.AssetPicker.Storage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Modules.AssetSystem.Editor.AssetPicker.View
{
    public class AssetRefValidatorWindow : EditorWindow
    {
        [Serializable]
        private class Hit
        {
            public string assetPath;
            public UnityEngine.Object contextObj;
            public string objectPath;
            public string componentType;
            public string fieldPath;
            public string value;
        }

        private Vector2 _scroll;
        private readonly List<Hit> _results = new();
        private static readonly string AssetRefTypeName = "AssetRef";

        [MenuItem("Tools/AssetRef Validator")]
        private static void Open()
        {
            var wnd = GetWindow<AssetRefValidatorWindow>("AssetRef Validator");
            wnd.RebuildNow();
        }
        
        public static void RequestRebuild()
        {
            var open = Resources.FindObjectsOfTypeAll<AssetRefValidatorWindow>();
            if (open != null && open.Length > 0)
            {
                var wnd = open[0];
                EditorApplication.delayCall += wnd.RebuildNow;
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("AssetRef that missing assets", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Results: {_results.Count}", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var h in _results)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"Asset: {h.assetPath}");
                    EditorGUILayout.LabelField($"Object: {h.objectPath}");
                    EditorGUILayout.LabelField($"Component: {h.componentType}");
                    EditorGUILayout.LabelField($"Field: {h.fieldPath}");
                    EditorGUILayout.LabelField($"Value: {h.value}");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Ping"))
                            EditorGUIUtility.PingObject(h.contextObj);
                        if (GUILayout.Button("Reveal"))
                            EditorUtility.RevealInFinder(h.assetPath);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void RebuildNow()
        {
            ScanProject();
            Repaint();
        }

        private void ScanProject()
        {
            _results.Clear();
            var componentTypes = CacheComponentTypesWithAssetRef();
            var soTypes = CacheScriptableObjectTypesWithAssetRef();
            FindInScriptableObjects(soTypes);
            FindInPrefabs(componentTypes);
            FindInScenes(componentTypes);
        }

        private void FindInPrefabs(HashSet<Type> componentTypes)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                EditorUtility.DisplayProgressBar("Scanning Prefabs", path, (float)i / guids.Length);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;

                    foreach (var comp in root.GetComponentsInChildren<Component>(true))
                    {
                        if (comp == null) continue;
                        var t = comp.GetType();
                        if (!componentTypes.Contains(t)) continue;

                        ScanObjectAssetRefs(comp, (fieldPath, idVal, hasInstance) =>
                        {
                            if (!string.IsNullOrEmpty(idVal) && IsBrokenPath(idVal))
                            {
                                _results.Add(new Hit
                                {
                                    assetPath = path,
                                    contextObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path),
                                    objectPath = GetHierarchyPath((comp as Component)?.transform),
                                    componentType = t.Name,
                                    fieldPath = fieldPath,
                                    value = idVal
                                });
                            }
                        });
                    }
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        private void FindInScriptableObjects(HashSet<Type> soTypes)
        {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                EditorUtility.DisplayProgressBar("Scanning ScriptableObjects", path, (float)i / guids.Length);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj == null) continue;
                var t = obj.GetType();
                if (!soTypes.Contains(t)) continue;

                ScanObjectAssetRefs(obj, (fieldPath, idVal, hasInstance) =>
                {
                    if (!string.IsNullOrEmpty(idVal) && IsBrokenPath(idVal))
                    {
                        _results.Add(new Hit
                        {
                            assetPath = path,
                            contextObj = obj,
                            objectPath = obj.name,
                            componentType = t.Name,
                            fieldPath = fieldPath,
                            value = idVal
                        });
                    }
                });
            }
            EditorUtility.ClearProgressBar();
        }

        private void FindInScenes(HashSet<Type> componentTypes)
        {
            var guids = AssetDatabase.FindAssets("t:Scene");
            var active = SceneManager.GetActiveScene();

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                EditorUtility.DisplayProgressBar("Scanning Scenes", path, (float)i / guids.Length);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var comp in root.GetComponentsInChildren<Component>(true))
                        {
                            if (comp == null) continue;
                            var t = comp.GetType();
                            if (!componentTypes.Contains(t)) continue;

                            ScanObjectAssetRefs(comp, (fieldPath, idVal, hasInstance) =>
                            {
                                if (!string.IsNullOrEmpty(idVal) && (!hasInstance || IsBrokenPath(idVal)))
                                {
                                    _results.Add(new Hit
                                    {
                                        assetPath = path,
                                        contextObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path),
                                        objectPath = GetHierarchyPath(comp.transform),
                                        componentType = t.Name,
                                        fieldPath = fieldPath,
                                        value = idVal
                                    });
                                }
                            });
                        }
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (active.IsValid())
                SceneManager.SetActiveScene(active);

            EditorUtility.ClearProgressBar();
        }

        private static HashSet<Type> CacheComponentTypesWithAssetRef()
        {
            var result = new HashSet<Type>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (!t.IsAbstract && HasAssetRefField(t))
                    result.Add(t);
            }
            return result;
        }

        private static HashSet<Type> CacheScriptableObjectTypesWithAssetRef()
        {
            var result = new HashSet<Type>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (!t.IsAbstract && HasAssetRefField(t))
                    result.Add(t);
            }
            return result;
        }

        private static bool HasAssetRefField(Type host)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in host.GetFields(flags))
            {
                if (FieldContainsAssetRef(f.FieldType))
                    return true;
            }
            return false;
        }

        private static bool FieldContainsAssetRef(Type t)
        {
            if (t == null) return false;
            if (IsAssetRefType(t)) return true;
            if (typeof(IList).IsAssignableFrom(t))
            {
                if (t.IsArray) return FieldContainsAssetRef(t.GetElementType());
                if (t.IsGenericType) return FieldContainsAssetRef(t.GetGenericArguments()[0]);
            }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(t) && t.IsClass == false && t.IsValueType && !t.IsPrimitive && !t.IsEnum)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var f in t.GetFields(flags))
                    if (FieldContainsAssetRef(f.FieldType)) return true;
            }
            return false;
        }

        private static bool IsAssetRefType(Type t)
        {
            if (t == null) return false;
            if (t.Name == AssetRefTypeName) return true;
            if (t.IsGenericType && t.Name.StartsWith(AssetRefTypeName, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void ScanObjectAssetRefs(UnityEngine.Object host, Action<string, string, bool> onAssetRefFound)
        {
            if (host == null) return;

            try
            {
                var so = new SerializedObject(host);
                so.UpdateIfRequiredOrScript();

                var it = so.GetIterator();
                var enterChildren = true;

                while (it.NextVisible(enterChildren))
                {
                    enterChildren = true;

                    if ((it.propertyType == SerializedPropertyType.Generic ||
                         it.propertyType == SerializedPropertyType.ManagedReference) &&
                        it.type.StartsWith(AssetRefTypeName, StringComparison.Ordinal))
                    {
                        var copy = it.Copy();
                        var end = it.GetEndProperty();

                        string pathVal = null;
                        bool hasInstance = false;

                        while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
                        {
                            if (copy.depth <= it.depth) break;

                            if (copy.name == "_instance" && copy.propertyType == SerializedPropertyType.ObjectReference)
                                hasInstance = copy.objectReferenceValue != null;

                            if (copy.name == "Path" && copy.propertyType == SerializedPropertyType.String)
                                pathVal = copy.stringValue;
                        }

                        if (!string.IsNullOrEmpty(pathVal))
                            onAssetRefFound(it.propertyPath, pathVal, hasInstance);

                        enterChildren = false;
                    }
                }
            }
            catch { }
        }

        private static bool IsBrokenPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            var p = path.Trim().Replace('\\', '/');

            if (p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p) == null;

            // кастомные провайдеры через реестр
            if (AssetRefRegistry.TryResolve(p, typeof(UnityEngine.Object), out var obj, out _))
                return false;

            // Resources: допускаем "foo/bar", "foo/bar.png", а также ошибочную "Resources/foo/bar(.ext)"
            var res = p.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase) ? p.Substring("Resources/".Length) : p;

            var obj1 = Resources.Load(res);
            if (obj1 != null) return false;

            var noExt = Path.ChangeExtension(res, null);
            if (!string.Equals(noExt, res, StringComparison.Ordinal))
            {
                var obj2 = Resources.Load(noExt);
                if (obj2 != null) return false;
            }

            return true;
        }
        
        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "(no transform)";
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
    }
}
