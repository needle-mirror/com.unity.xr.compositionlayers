using System;
using UnityEditor;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// The scene list the splash scene is added to, and the folder its copy is created in.
    /// </summary>
    readonly struct SplashSceneTarget
    {
        internal SplashSceneTarget(
            string folder, Func<EditorBuildSettingsScene[]> getScenes, Action<EditorBuildSettingsScene[]> setScenes)
        {
            Folder = folder;
            GetScenes = getScenes;
            SetScenes = setScenes;
        }

        /// <summary>
        /// The project's Build Settings scene list and the default splash scene folder.
        /// </summary>
        internal static SplashSceneTarget Project => new(
            SplashSceneSetup.k_DefaultFolder,
            () => EditorBuildSettings.scenes,
            scenes => EditorBuildSettings.scenes = scenes);

        internal string Folder { get; }

        internal Func<EditorBuildSettingsScene[]> GetScenes { get; }

        internal Action<EditorBuildSettingsScene[]> SetScenes { get; }
    }
}
