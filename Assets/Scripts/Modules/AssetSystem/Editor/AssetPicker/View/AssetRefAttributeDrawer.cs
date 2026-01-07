using Modules.AssetSystem.Attributes;
using Modules.AssetSystem.Editor.AssetPicker.Storage;
using UnityEditor;
using UnityEngine;

namespace Modules.AssetSystem.Editor.AssetPicker.View
{
    [CustomPropertyDrawer(typeof(AssetRefAttribute))]
    public sealed class AssetRefAttributePropertyDrawer : PropertyDrawer
    {
        private const float SelectButtonWidht = 60;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "AssetRef only works on string");
                return;
            }

            var attr = (AssetRefAttribute)attribute;
            var type = attr.TargetType;

            EditorGUI.BeginProperty(position, label, property);

            var objectFieldRect = new Rect(position.x, position.y, position.width - SelectButtonWidht, position.height);
            var selectButtonRect = new Rect(position.x + position.width - SelectButtonWidht, position.y,
                SelectButtonWidht, position.height);

            Object currentObj = null;
            if (!string.IsNullOrEmpty(property.stringValue))
                AssetRefRegistry.TryResolve(property.stringValue, type, out currentObj, out _);

            var newObj = EditorGUI.ObjectField(objectFieldRect, label, currentObj, type, false);
            if (newObj != currentObj)
            {
                if (newObj == null) property.stringValue = "";
                else if (AssetRefRegistry.TryMakeAssetRef(newObj, out var aref))
                    property.stringValue = aref;
                else
                    EditorUtility.DisplayDialog("Unsupported", "This asset is not in supported providers", "OK");
            }

            if (GUI.Button(selectButtonRect, "Select"))
            {
                var rect = GUIUtility.GUIToScreenRect(position);
                AssetRefPickerWindow.Show(type, (obj, assetRef) =>
                {
                    property.serializedObject.Update();
                    property.stringValue = assetRef;
                    property.serializedObject.ApplyModifiedProperties();
                }, rect);
            }

            EditorGUI.EndProperty();
        }
    }
}