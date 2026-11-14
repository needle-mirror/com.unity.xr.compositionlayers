using Unity.Scripting.LifecycleManagement;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Object = UnityEngine.Object;
#endif

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Finds the project's <see cref="CompositionLayersRuntimeSettings"/>, or in-memory defaults when it has none.
    /// </summary>
    static partial class RuntimeSettingsLookup
    {
        [AutoStaticsCleanup]
        static CompositionLayersRuntimeSettings s_Override;

        // A DontSave object survives entering Play mode, so clearing this reference would leak one instance each time.
        [NoAutoStaticsCleanup]
        static CompositionLayersRuntimeSettings s_Defaults;

#if !UNITY_EDITOR
        [AutoStaticsCleanup]
        static CompositionLayersRuntimeSettings s_Registered;
#endif

        /// <summary>
        /// The settings the package uses in this Editor or player.
        /// </summary>
        internal static CompositionLayersRuntimeSettings Current
        {
            get
            {
#if UNITY_EDITOR
                return Find(RuntimeSettingsLocations.Default, PlayerSettings.GetPreloadedAssets());
#else
                if (s_Override != null)
                    return s_Override;

                return s_Registered != null ? s_Registered : GetDefaults();
#endif
            }
        }

        /// <summary>
        /// Replaces the lookup result, so tests supply settings without touching the project.
        /// </summary>
        internal static CompositionLayersRuntimeSettings Override
        {
            get => s_Override;
            set => s_Override = value;
        }

#if !UNITY_EDITOR
        internal static void Register(CompositionLayersRuntimeSettings settings)
        {
            s_Registered = settings;
        }

        internal static void Unregister(CompositionLayersRuntimeSettings settings)
        {
            if (s_Registered == settings)
                s_Registered = null;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Finds the settings for a build, or in-memory defaults when there is no asset. Never creates a file.
        /// </summary>
        internal static CompositionLayersRuntimeSettings Find(
            RuntimeSettingsLocations locations, Object[] preloadedAssets)
        {
            if (s_Override != null)
                return s_Override;

            return TryFindAsset(locations, preloadedAssets, out var settings) ? settings : GetDefaults();
        }

        /// <summary>
        /// Finds the settings asset, preferring one outside <c>Resources</c> over one an earlier version left inside.
        /// </summary>
        internal static bool TryFindAsset(
            RuntimeSettingsLocations locations, Object[] preloadedAssets, out CompositionLayersRuntimeSettings settings)
        {
            return TryFindNewAsset(locations, preloadedAssets, out settings)
                || TryFindOldAsset(locations, out settings);
        }

        /// <summary>
        /// Finds a settings asset outside any <c>Resources</c> folder: in Preloaded Assets, at the default path, then
        /// by type.
        /// </summary>
        internal static bool TryFindNewAsset(
            RuntimeSettingsLocations locations, Object[] preloadedAssets, out CompositionLayersRuntimeSettings settings)
        {
            return TryFindNewAssetInKnownPlaces(locations, preloadedAssets, out settings)
                || TryFindByType(locations, inResourcesFolder: false, out settings);
        }

        /// <summary>
        /// Finds a settings asset outside any <c>Resources</c> folder in Preloaded Assets or at the default path. Never
        /// searches the project.
        /// </summary>
        internal static bool TryFindNewAssetInKnownPlaces(
            RuntimeSettingsLocations locations, Object[] preloadedAssets, out CompositionLayersRuntimeSettings settings)
        {
            foreach (var asset in preloadedAssets)
            {
                if (asset is CompositionLayersRuntimeSettings preloadedSettings
                    && preloadedSettings != null
                    && !RuntimeSettingsLocations.IsInResourcesFolder(AssetDatabase.GetAssetPath(preloadedSettings)))
                {
                    settings = preloadedSettings;
                    return true;
                }
            }

            settings = AssetDatabase.LoadAssetAtPath<CompositionLayersRuntimeSettings>(locations.DefaultAssetPath);
            return settings != null;
        }

        /// <summary>
        /// Finds a settings asset an earlier version left in a <c>Resources</c> folder: at the path it used, then by
        /// type.
        /// </summary>
        internal static bool TryFindOldAsset(
            RuntimeSettingsLocations locations, out CompositionLayersRuntimeSettings settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<CompositionLayersRuntimeSettings>(locations.LegacyAssetPath);
            if (settings != null)
                return true;

            return TryFindByType(locations, inResourcesFolder: true, out settings);
        }

        /// <summary>
        /// Searches the project for a settings asset inside or outside a <c>Resources</c> folder.
        /// </summary>
        internal static bool TryFindByType(
            RuntimeSettingsLocations locations, bool inResourcesFolder, out CompositionLayersRuntimeSettings settings)
        {
            const string typeFilter = "t:" + nameof(CompositionLayersRuntimeSettings);
            foreach (var guid in AssetDatabase.FindAssets(typeFilter, new[] { locations.SearchFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (RuntimeSettingsLocations.IsInResourcesFolder(path) != inResourcesFolder)
                    continue;

                settings = AssetDatabase.LoadAssetAtPath<CompositionLayersRuntimeSettings>(path);
                if (settings != null)
                    return true;
            }

            settings = null;
            return false;
        }
#endif

        static CompositionLayersRuntimeSettings GetDefaults()
        {
            if (s_Defaults != null)
                return s_Defaults;

            s_Defaults = ScriptableObject.CreateInstance<CompositionLayersRuntimeSettings>();
            s_Defaults.name = "Default " + nameof(CompositionLayersRuntimeSettings);
            s_Defaults.hideFlags = HideFlags.DontSave;
            return s_Defaults;
        }
    }
}
