using UnityEditor;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Rendering
{
    [InitializeOnLoad]
    /// <summary>
    /// Automates the Loading of Graphics Settings
    /// </summary>
    public static class GraphicsSettingsLoader
    {
        // Check for Standalone Builds and Add Uber Shader if detected.
        static GraphicsSettingsLoader()
        {
            if ((EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64) && CompositionLayersRuntimeSettings.Instance.EmulationInStandalone)
            {
                AddShadersToGraphicsSettings();
            }
        }

        private static void AddShadersToGraphicsSettings()
        {
            if (GraphicsSettingsHelpers.AddAlwaysIncludedShaders(GraphicsSettingsHelpers.ShaderType.Uber))
            {
                Debug.Log("Standalone build target detected. Adding Uber shader to Graphics Settings for Emulation.");
            }
        }
    }
}
