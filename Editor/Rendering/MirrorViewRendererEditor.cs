using UnityEditor;

namespace Unity.XR.CompositionLayers.Rendering.Editor
{
    [CustomEditor(typeof(MirrorViewRenderer))]
    public class MirrorViewRendererEditor : UnityEditor.Editor
    {
        SerializedProperty AlphaModeProperty;

        void OnEnable()
        {
            AlphaModeProperty = serializedObject.FindProperty("AlphaMode");

            GraphicsSettingsHelpers.AddAlwaysIncludedShaders(GraphicsSettingsHelpers.ShaderType.BlitCopyHDR);
            GraphicsSettingsHelpers.AddAlwaysIncludedShaders(GraphicsSettingsHelpers.ShaderType.Uber);
        }

        public override void OnInspectorGUI()
        {
            var mirrorViewRenderer = (MirrorViewRenderer)target;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            if (AlphaModeProperty != null)
                EditorGUILayout.PropertyField(AlphaModeProperty);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
