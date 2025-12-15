using Modules.AssetSystem.Attributes;
using Modules.AssetSystem.Editor.AssetPicker.Storage;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace Modules.AssetSystem.Editor.AssetPicker.View
{
    [CustomPropertyDrawer(typeof(AssetRefAttribute))]
    public sealed class AssetRefAttributePropertyDrawer : PropertyDrawer
    {
        private const float SelectButtonWidth = 64f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var h = EditorGUIUtility.singleLineHeight;
            var s = EditorGUIUtility.standardVerticalSpacing;
            return h + s + h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var pathProp = property.FindPropertyRelative("Path");
            var guidProp = property.FindPropertyRelative("GUID");

            EditorGUI.BeginProperty(position, label, property);

            var lineH = EditorGUIUtility.singleLineHeight;
            var vsp   = EditorGUIUtility.standardVerticalSpacing;

            var pathRect = new Rect(position.x, position.y, position.width, lineH);
            var objRect  = new Rect(position.x, position.y + lineH + vsp, position.width - SelectButtonWidth - 4f, lineH);
            var btnRect  = new Rect(objRect.xMax + 4f, objRect.y, SelectButtonWidth, lineH);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(pathRect, label.text + " Path", pathProp.stringValue ?? string.Empty);
            EditorGUI.EndDisabledGroup();
            
            Object current = null;
            if (!string.IsNullOrEmpty(guidProp.stringValue))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guidProp.stringValue);
                if (!string.IsNullOrEmpty(assetPath))
                    current = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
            
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(objRect, GUIContent.none, current, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.Update();
                if (newObj == null)
                {
                    guidProp.stringValue = string.Empty;
                    pathProp.stringValue = string.Empty;
                }
                else if (AssetRefRegistry.TryMakeAssetRef(newObj, out var aref))
                {
                    var newPath = AssetDatabase.GetAssetPath(newObj);
                    var newGuid = AssetDatabase.AssetPathToGUID(newPath);
                    guidProp.stringValue = newGuid;
                    pathProp.stringValue = aref;
                }
                property.serializedObject.ApplyModifiedProperties();
                AssetRefValidatorWindow.RequestRebuild();
            }
            
            if (GUI.Button(btnRect, "Select"))
            {
                var screenRect = GUIUtility.GUIToScreenRect(btnRect);
                AssetRefPickerWindow.Show(typeof(GameObject), (obj, assetRef) => 
                {
                    property.serializedObject.Update();
                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    guidProp.stringValue = AssetDatabase.AssetPathToGUID(assetPath);
                    pathProp.stringValue = assetRef;
                    property.serializedObject.ApplyModifiedProperties();
                    AssetRefValidatorWindow.RequestRebuild();
                }, screenRect);
            }

            EditorGUI.EndProperty();
        }
    }
}
