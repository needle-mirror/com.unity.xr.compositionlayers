using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.XR.CompositionLayers.Editor
{
    [CustomEditor(typeof(CompositionLayersPreferences))]
    class CompositionLayersPreferencesEditor : UnityEditor.Editor
    {
        const string k_DeprecationMessage = "This asset is deprecated as of Composition Layers 7.0.0-pre.1. " +
            "Its settings are no longer used, and it is safe to delete.";

        public override VisualElement CreateInspectorGUI()
        {
            return new HelpBox(k_DeprecationMessage, HelpBoxMessageType.Warning);
        }
    }
}
