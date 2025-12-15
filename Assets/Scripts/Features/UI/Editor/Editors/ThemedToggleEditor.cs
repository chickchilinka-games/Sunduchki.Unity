using Features.UI.Components;
using UnityEditor;
using UnityEditor.UI;

namespace Features.UI.Editor.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ThemedToggle), true)]
    public class ThemedToggleEditor : ToggleEditor
    {
        private SerializedProperty _stylerOn;
        private SerializedProperty _stylerOff;
        private SerializedProperty _graphicsWhenOn;
        private SerializedProperty _graphicsWhenOff;
        

        protected override void OnEnable()
        {
            base.OnEnable();
            _stylerOn = serializedObject.FindProperty("_stylerOn");
            _stylerOff = serializedObject.FindProperty("_stylerOff");
            _graphicsWhenOn = serializedObject.FindProperty("_graphicsWhenOn");
            _graphicsWhenOff = serializedObject.FindProperty("_graphicsWhenOff");
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_stylerOn);
            EditorGUILayout.PropertyField(_stylerOff);
            EditorGUILayout.PropertyField(_graphicsWhenOn);
            EditorGUILayout.PropertyField(_graphicsWhenOff);
            serializedObject.ApplyModifiedProperties();
        }
    }
}