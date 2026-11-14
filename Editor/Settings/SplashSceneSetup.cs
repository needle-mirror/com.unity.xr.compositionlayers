using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// Keeps the Composition Layers splash scene first in a build's scene list while the splash is enabled.
    /// </summary>
    static class SplashSceneSetup
    {
        internal const string k_TemplatePath =
            "Packages/com.unity.xr.compositionlayers/Runtime/Scenes/CompositionSplash.unity";

        internal const string k_DefaultFolder = "Assets/XR/CompositionLayers";

        internal const string k_SceneFileName = "CompositionSplash.unity";

        internal const string k_PlayerSplashWarning = "The Splash Screen should be disabled in the Player Settings " +
            "to use the Composition Layers Splash Screen.";

        internal const string k_SceneMissingError = "The Composition Layers Splash Scene is missing from the Build " +
            "Settings.";

        internal const string k_SceneNotFirstError = "The Composition Layers Splash Scene should be at the first " +
            "index in the Build Settings.";

        internal const string k_NoSceneAfterWarning = "There is no scene assigned after the splash scene in the " +
            "Build Settings. The splash screen will display indefinitely.";

        /// <summary>
        /// Whether the Player Settings show a splash screen of their own.
        /// </summary>
        internal static bool IsPlayerSplashShown =>
            PlayerSettings.SplashScreen.show || PlayerSettings.virtualRealitySplashScreen != null;

        /// <summary>
        /// Adds the splash scene first in the list when the splash is enabled, copying the template into the folder if
        /// it is not there yet, or removes it when the splash is disabled.
        /// </summary>
        internal static EditorBuildSettingsScene[] UpdateScenes(
            bool splashEnabled, EditorBuildSettingsScene[] scenes, string folder)
        {
            var splashScenePath = GetScenePath(folder);
            var updatedScenes = new List<EditorBuildSettingsScene>(scenes.Length + 1);
            if (splashEnabled && TryCopyTemplateIfMissing(folder))
                updatedScenes.Add(new EditorBuildSettingsScene(splashScenePath, true));

            foreach (var scene in scenes)
            {
                if (scene.path != splashScenePath)
                    updatedScenes.Add(scene);
            }

            return updatedScenes.ToArray();
        }

        /// <summary>
        /// The problems with the splash setup, in display order.
        /// </summary>
        internal static List<(string text, HelpBoxMessageType type)> GetMessages(
            bool splashEnabled, bool playerSplashShown, EditorBuildSettingsScene[] scenes, string folder)
        {
            var messages = new List<(string, HelpBoxMessageType)>();
            if (!splashEnabled)
                return messages;

            if (playerSplashShown)
                messages.Add((k_PlayerSplashWarning, HelpBoxMessageType.Warning));

            var splashIndex = IndexOfScene(scenes, GetScenePath(folder));
            if (splashIndex < 0)
                messages.Add((k_SceneMissingError, HelpBoxMessageType.Error));
            else if (splashIndex > 0)
                messages.Add((k_SceneNotFirstError, HelpBoxMessageType.Error));
            else if (!HasEnabledSceneAfter(scenes, splashIndex))
                messages.Add((k_NoSceneAfterWarning, HelpBoxMessageType.Warning));

            return messages;
        }

        internal static string GetScenePath(string folder) => folder + "/" + k_SceneFileName;

        static bool TryCopyTemplateIfMissing(string folder)
        {
            var splashScenePath = GetScenePath(folder);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(splashScenePath) != null)
                return true;

            RuntimeSettingsSetup.CreateFolders(folder);
            if (AssetDatabase.CopyAsset(k_TemplatePath, splashScenePath))
                return true;

            Debug.LogError($"XR Composition Layers could not copy the splash scene from {k_TemplatePath} to " +
                $"{splashScenePath}, so the splash screen is not added to the build.");
            return false;
        }

        static int IndexOfScene(EditorBuildSettingsScene[] scenes, string path)
        {
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path)
                    return i;
            }

            return -1;
        }

        static bool HasEnabledSceneAfter(EditorBuildSettingsScene[] scenes, int index)
        {
            for (var i = index + 1; i < scenes.Length; i++)
            {
                if (scenes[i].enabled)
                    return true;
            }

            return false;
        }
    }
}
