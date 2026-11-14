using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.XR.CompositionLayers;
using UnityEngine;

namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
    /// <summary>
    /// Finds and deletes leftover <c>CompositionLayersPreferences</c> assets. The search result is cached until the
    /// asset database changes, because Project Validation checks its rules every second while it is open.
    /// </summary>
    class CompositionLayersPreferencesCheck
    {
        const string k_AssetFilter = "t:" + nameof(CompositionLayersPreferences);

        // Only ever increases, so a cache stamped with an earlier value is always stale.
        [NoAutoStaticsCleanup]
        static int s_AssetDatabaseChangeCount;

        readonly string[] m_SearchFolders;
        bool m_HasLeftoverAssets;
        int m_CachedChangeCount = -1;

        internal CompositionLayersPreferencesCheck(params string[] searchFolders)
        {
            m_SearchFolders = searchFolders;
        }

        internal bool HasLeftoverAssets()
        {
            if (m_CachedChangeCount != s_AssetDatabaseChangeCount)
            {
                m_HasLeftoverAssets = AssetDatabase.FindAssets(k_AssetFilter, m_SearchFolders).Length > 0;
                m_CachedChangeCount = s_AssetDatabaseChangeCount;
            }

            return m_HasLeftoverAssets;
        }

        internal void DeleteLeftoverAssets()
        {
            var guids = AssetDatabase.FindAssets(k_AssetFilter, m_SearchFolders);
            var deletedPaths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.DeleteAsset(path))
                    deletedPaths.Add(path);
            }

            if (deletedPaths.Count == 0)
                return;

            Debug.Log("Deleted leftover Composition Layers preferences: " + string.Join(", ", deletedPaths));
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
}
