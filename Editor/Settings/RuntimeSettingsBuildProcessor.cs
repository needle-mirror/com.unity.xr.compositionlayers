using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// Checks the <see cref="CompositionLayersRuntimeSettings"/> asset, which holds the package shaders and the splash
    /// settings, before a player build. Never changes the project; the settings page and Project Validation do that.
    /// </summary>
    class RuntimeSettingsBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Validate(RuntimeSettingsLocations.Default, PlayerSettings.GetPreloadedAssets());
        }

        /// <summary>
        /// Fails the build if settings that change what the player does would be ignored or might lose to another
        /// preloaded settings asset. Warns if the settings are otherwise not set up.
        /// </summary>
        /// <exception cref="BuildFailedException">Settings that turn on the splash screen or Emulation In Standalone
        /// aren't in Preloaded Assets, or aren't the only settings there.</exception>
        internal static void Validate(RuntimeSettingsLocations locations, Object[] preloadedAssets)
        {
            if (RuntimeSettingsSetup.IsSetUp(locations, preloadedAssets))
                return;

            if (RuntimeSettingsLookup.TryFindAsset(locations, preloadedAssets, out var settings)
                && Array.IndexOf(preloadedAssets, settings) < 0
                && ChangesPlayer(settings))
            {
                throw new BuildFailedException(RuntimeSettingsMessages.k_BuildIgnoresSettings);
            }

            if (HasConflictingPreloadedSettings(preloadedAssets))
                throw new BuildFailedException(RuntimeSettingsMessages.k_BuildConflictingSettings);

            Debug.LogWarning(RuntimeSettingsMessages.k_BuildNotSetUp);
        }

        // Only these settings change what a player does, and both are off by default, so an untouched asset that 6.x
        // created, as every Meta OpenXR Android build did, only warns.
        static bool ChangesPlayer(CompositionLayersRuntimeSettings settings)
        {
            return settings.EnableSplashScreen || settings.IsEmulationInStandaloneEnabled;
        }

        // A player registers every preloaded settings asset as it loads, so which one it ends up using isn't defined.
        static bool HasConflictingPreloadedSettings(Object[] preloadedAssets)
        {
            CompositionLayersRuntimeSettings firstSettings = null;
            var hasOtherSettings = false;
            var anyChangesPlayer = false;
            foreach (var asset in preloadedAssets)
            {
                if (asset is not CompositionLayersRuntimeSettings settings || settings == null)
                    continue;

                if (firstSettings == null)
                    firstSettings = settings;
                else if (settings != firstSettings)
                    hasOtherSettings = true;

                anyChangesPlayer |= ChangesPlayer(settings);
            }

            return hasOtherSettings && anyChangesPlayer;
        }
    }
}
