using UnityEditor;

namespace Unity.XR.CompositionLayers.Rendering.Editor
{
#pragma warning disable CS0618
    /// <summary>
    /// Creates the MirrorViewRenderer editor.
    /// </summary>
    [CustomEditor(typeof(MirrorViewRenderer))]
    [System.Obsolete("MirrorViewRendererEditor is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public class MirrorViewRendererEditor : UnityEditor.Editor
    {
        SerializedProperty AlphaModeProperty;

        /// <summary>
        /// Initializes the editor when it is enabled.
        /// </summary>
        void OnEnable()
        {
            AlphaModeProperty = serializedObject.FindProperty("AlphaMode");
        }

        /// <summary>
        /// Draws the custom inspector GUI for the <see cref="MirrorViewRenderer"/>.
        /// </summary>
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
#pragma warning restore CS0618
}
