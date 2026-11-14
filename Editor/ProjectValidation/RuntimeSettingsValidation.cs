using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
#pragma warning disable CS0618 // CompositionLayerBuildValidationRule is obsolete for use outside the package.
    /// <summary>
    /// Project Validation rules for the <see cref="CompositionLayersRuntimeSettings"/> asset: an error, one per reason,
    /// until it is set up for builds, and a warning while an earlier version's copy is left in a <c>Resources</c>
    /// folder.
    /// </summary>
    class RuntimeSettingsValidation
    {
        const string k_AssetFileName = RuntimeSettingsMessages.k_AssetFileName;
        const string k_UpgradeGuideUrl = "https://docs.unity3d.com/Packages/com.unity.xr.compositionlayers@7.0/" +
            "manual/upgrade-guide-7.0.html#move-the-runtime-settings-out-of-resources";

        // Only ever increases, so a cache stamped with an earlier value is always stale.
        [NoAutoStaticsCleanup]
        static int s_AssetDatabaseChangeCount;

        readonly RuntimeSettingsLocations m_Locations;
        readonly Func<Object[]> m_GetPreloadedAssets;
        readonly Action<Object[]> m_SetPreloadedAssets;
        bool m_HasOldAsset;
        CompositionLayersRuntimeSettings m_NewAssetFoundBySearch;
        int m_CachedChangeCount = -1;

        internal RuntimeSettingsValidation(
            RuntimeSettingsLocations locations, Func<Object[]> getPreloadedAssets, Action<Object[]> setPreloadedAssets)
        {
            m_Locations = locations;
            m_GetPreloadedAssets = getPreloadedAssets;
            m_SetPreloadedAssets = setPreloadedAssets;
        }

        internal static CompositionLayerBuildValidationRule CreateMigrateRule(RuntimeSettingsValidation validation)
        {
            return CreateSetupRule(
                validation,
                RuntimeSettingsMessages.NeedsMigrating(validation.m_Locations),
                validation.NeedsMigrating,
                GetFixItMessage(RuntimeSettingsMessages.k_MigrateButton));
        }

        internal static CompositionLayerBuildValidationRule CreateMissingRule(RuntimeSettingsValidation validation)
        {
            return CreateSetupRule(
                validation,
                RuntimeSettingsMessages.Missing(validation.m_Locations),
                validation.IsMissing,
                GetFixItMessage(RuntimeSettingsMessages.k_CreateButton));
        }

        internal static CompositionLayerBuildValidationRule CreateNotPreloadedRule(RuntimeSettingsValidation validation)
        {
            return CreateSetupRule(
                validation,
                RuntimeSettingsMessages.k_NotPreloaded,
                validation.IsNotPreloaded,
                GetFixItMessage(RuntimeSettingsMessages.k_SetUpButton));
        }

        static string GetFixItMessage(string pageButton)
        {
            return $"Same as {pageButton} on the Composition Layers settings page.";
        }

        // Each setup rule fails in a different case, and all run the same setup step.
        static CompositionLayerBuildValidationRule CreateSetupRule(
            RuntimeSettingsValidation validation, string message, Func<bool> fails, string fixItMessage)
        {
            return new CompositionLayerBuildValidationRule
            {
                Message = message,
                CheckPredicate = () => !fails(),
                FixIt = validation.SetUp,
                FixItAutomatic = true,
                FixItMessage = fixItMessage,
                HelpLink = k_UpgradeGuideUrl,
                Error = true,
                requiresActiveManager = false,
            };
        }

        internal static CompositionLayerBuildValidationRule CreateOldCopyRule(RuntimeSettingsValidation validation)
        {
            return new CompositionLayerBuildValidationRule
            {
                Message = "Duplicate settings assets. Composition Layers uses " +
                    $"{validation.m_Locations.DefaultAssetPath}. The old {k_AssetFileName} in your project's " +
                    "Resources folder is safe to delete.",
                CheckPredicate = () => !validation.HasOldAsset() || !validation.HasNewAsset(),
                FixIt = validation.DeleteOldCopy,
                FixItAutomatic = true,
                FixItMessage = $"Delete the old {k_AssetFileName}.",
                HelpLink = k_UpgradeGuideUrl,
                Error = false,
                requiresActiveManager = false,
            };
        }

        internal bool HasOldAsset()
        {
            RefreshProjectSearch();
            return m_HasOldAsset;
        }

        internal bool HasNewAsset() => TryFindNewAsset(m_GetPreloadedAssets(), out _);

        bool NeedsMigrating() => !HasNewAsset() && HasOldAsset();

        bool IsMissing() => !HasNewAsset() && !HasOldAsset();

        bool IsNotPreloaded()
        {
            var preloadedAssets = m_GetPreloadedAssets();
            return TryFindNewAsset(preloadedAssets, out var settings)
                && !RuntimeSettingsSetup.IsSetUp(settings, preloadedAssets);
        }

        // Finds the asset in the same order as RuntimeSettingsLookup.TryFindNewAsset. Preloaded Assets can change
        // without an asset database change, so only the project search is cached.
        bool TryFindNewAsset(Object[] preloadedAssets, out CompositionLayersRuntimeSettings settings)
        {
            if (RuntimeSettingsLookup.TryFindNewAssetInKnownPlaces(m_Locations, preloadedAssets, out settings))
                return true;

            RefreshProjectSearch();
            settings = m_NewAssetFoundBySearch;
            return settings != null;
        }

        // The searches cover the whole project, so they're cached until the asset database changes. Project Validation
        // checks its rules every second while it is open.
        void RefreshProjectSearch()
        {
            if (m_CachedChangeCount == s_AssetDatabaseChangeCount)
                return;

            m_HasOldAsset = RuntimeSettingsLookup.TryFindOldAsset(m_Locations, out _);
            RuntimeSettingsLookup.TryFindByType(m_Locations, inResourcesFolder: false, out m_NewAssetFoundBySearch);
            m_CachedChangeCount = s_AssetDatabaseChangeCount;
        }

        // Logs a failure instead of throwing, so Fix All goes on to the other rules.
        void SetUp()
        {
            try
            {
                if (RuntimeSettingsSetup.SetUp(m_Locations, m_GetPreloadedAssets(), out _, out var preloadedAssets))
                    m_SetPreloadedAssets(preloadedAssets);
            }
            catch (BuildFailedException exception)
            {
                Debug.LogError(exception.Message);
            }
        }

        void DeleteOldCopy()
        {
            if (!HasNewAsset() || !RuntimeSettingsLookup.TryFindOldAsset(m_Locations, out var oldSettings))
                return;

            var oldPath = AssetDatabase.GetAssetPath(oldSettings);
            RemoveFromPreloadedAssets(oldSettings);
            if (!AssetDatabase.DeleteAsset(oldPath))
            {
                Debug.LogError($"XR Composition Layers could not delete {oldPath}.");
                return;
            }

            RuntimeSettingsSetup.DeleteEmptiedResourcesFolders(RuntimeSettingsSetup.GetFolder(oldPath));
        }

        // Otherwise Preloaded Assets would keep an empty entry for the deleted asset.
        void RemoveFromPreloadedAssets(Object asset)
        {
            var preloadedAssets = m_GetPreloadedAssets();
            if (Array.IndexOf(preloadedAssets, asset) < 0)
                return;

            var keptAssets = new List<Object>(preloadedAssets.Length);
            foreach (var preloadedAsset in preloadedAssets)
            {
                if (preloadedAsset != asset)
                    keptAssets.Add(preloadedAsset);
            }

            m_SetPreloadedAssets(keptAssets.ToArray());
        }

        class AssetDatabaseChangeListener : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                s_AssetDatabaseChangeCount++;
            }
        }
    }
#pragma warning restore CS0618
}
