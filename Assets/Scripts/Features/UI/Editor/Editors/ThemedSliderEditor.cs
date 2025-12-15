using Features.UI.Components;
using UnityEditor;
using UnityEditor.UI;

namespace Features.UI.Editor.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ThemedSlider), true)]
    public class ThemedSliderEditor : SliderEditor
    {
        private SerializedProperty _styler;

        protected override void OnEnable()
        {
            base.OnEnable();
            _styler = serializedObject.FindProperty("_styler");
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_styler);
            serializedObject.ApplyModifiedProperties();
        }
    }
}