using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// Custom editor for <see cref="CompositionLayersRuntimeSettings"/>.
    /// </summary>
    [CustomEditor(typeof(CompositionLayersRuntimeSettings))]
    [System.Obsolete("CompositionLayersRuntimeSettingsEditor is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public class CompositionLayersRuntimeSettingsEditor : UnityEditor.Editor
    {
        const string k_SettingsContainerName = "settings";
        const string k_NotInUseHelpBoxName = "not-in-use";

        const string k_SettingsPageMessage = "Edit these settings in Project Settings > XR Plug-in Management > " +
            "Composition Layers.";

        const string k_ScriptPropertyName = "m_Script";

        /// <summary>
        /// Creates the inspector: a link to the settings page, and, when the project uses a different settings asset,
        /// this asset's settings, read-only.
        /// </summary>
        /// <returns>The root of the inspector.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new HelpBox(k_SettingsPageMessage, HelpBoxMessageType.Info));
            root.Add(new Button(OpenSettingsPage) { text = "Open Project Settings" });
            if (target == RuntimeSettingsLookup.Current)
                return root;

            root.Add(new HelpBox(GetNotInUseMessage(), HelpBoxMessageType.Warning) { name = k_NotInUseHelpBoxName });
            var settings = new VisualElement { name = k_SettingsContainerName };
            var property = serializedObject.GetIterator();
            for (var enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
            {
                if (property.name != k_ScriptPropertyName)
                    settings.Add(new PropertyField(property.Copy()));
            }

            settings.SetEnabled(false);
            settings.Bind(serializedObject);
            root.Add(settings);
            return root;
        }

        /// <summary>
        /// Draws the custom inspector GUI for the <see cref="CompositionLayersRuntimeSettings"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(k_SettingsPageMessage, MessageType.Info);
            if (GUILayout.Button("Open Project Settings"))
                OpenSettingsPage();
        }

        // Names the asset in use, so users can compare the two.
        static string GetNotInUseMessage()
        {
            var pathInUse = AssetDatabase.GetAssetPath(RuntimeSettingsLookup.Current);
            var assetInUse = string.IsNullOrEmpty(pathInUse) ? "" : $": {pathInUse}";
            return $"As of Composition Layers 7.0.0-pre.1, a project uses one settings asset{assetInUse}. This asset " +
                "isn't used and is safe to delete. Its settings are shown read-only so you can compare them before " +
                "you delete it.";
        }

        static void OpenSettingsPage()
        {
            SettingsService.OpenProjectSettings(CompositionLayersSettingsProvider.k_SettingsPath);
        }
    }
}
