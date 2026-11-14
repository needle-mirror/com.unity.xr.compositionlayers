using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// Sets up the <see cref="CompositionLayersRuntimeSettings"/> asset for player builds. The only code that changes
    /// the project for the settings, and it runs only when the user asks.
    /// </summary>
    static class RuntimeSettingsSetup
    {
        const string k_MacOSFolderMetadataFileName = ".DS_Store";

        /// <summary>
        /// Moves the settings out of an earlier version's <c>Resources</c> folder, or creates them if the project has
        /// none, then references the package shaders and makes the asset the only settings in Preloaded Assets. Never
        /// overwrites a file, and leaves an old copy alone when the project already has new settings.
        /// </summary>
        /// <returns><see langword="true"/> if the project or the Preloaded Assets changed.</returns>
        /// <exception cref="BuildFailedException">A package shader cannot be found.</exception>
        internal static bool SetUp(
            RuntimeSettingsLocations locations,
            Object[] preloadedAssets,
            out CompositionLayersRuntimeSettings settings,
            out Object[] updatedPreloadedAssets)
        {
            var changed = false;
            if (!RuntimeSettingsLookup.TryFindNewAsset(locations, preloadedAssets, out settings))
            {
                if (!RuntimeSettingsLookup.TryFindOldAsset(locations, out settings))
                    settings = Create(locations);
                else if (!TryMoveToDefaultFolder(settings, locations))
                {
                    updatedPreloadedAssets = preloadedAssets;
                    return false;
                }

                changed = true;
            }

            if (AssignShaders(settings))
            {
                AssetDatabase.SaveAssetIfDirty(settings);
                changed = true;
            }

            changed |= TryRemoveOtherSettings(preloadedAssets, settings, out var cleanedPreloadedAssets);
            changed |= TryAppendMissing(cleanedPreloadedAssets, settings, out updatedPreloadedAssets);
            return changed;
        }

        /// <summary>
        /// Whether <see cref="SetUp"/> has nothing left to do: the project has settings outside <c>Resources</c>, they
        /// reference the package shaders, and they are the only settings in Preloaded Assets.
        /// </summary>
        internal static bool IsSetUp(RuntimeSettingsLocations locations, Object[] preloadedAssets)
        {
            return RuntimeSettingsLookup.TryFindNewAsset(locations, preloadedAssets, out var settings)
                && IsSetUp(settings, preloadedAssets);
        }

        /// <summary>
        /// Whether these settings reference the package shaders and are the only settings in Preloaded Assets.
        /// </summary>
        internal static bool IsSetUp(CompositionLayersRuntimeSettings settings, Object[] preloadedAssets)
        {
            return HasPackageShaders(settings) && IsOnlySettingsPreloaded(preloadedAssets, settings);
        }

        /// <summary>
        /// Points the settings at the package shaders.
        /// </summary>
        /// <returns><see langword="true"/> if any reference changed.</returns>
        /// <exception cref="BuildFailedException">A package shader cannot be found.</exception>
        internal static bool AssignShaders(CompositionLayersRuntimeSettings settings)
        {
            var uber = FindShaderOrFailBuild(CompositionLayerConstants.UberShaderName);
            var colorScaleBias = FindShaderOrFailBuild(CompositionLayerConstants.ColorScaleBiasShaderName);
            var blitCopyHDR = FindShaderOrFailBuild(CompositionLayerConstants.BlitCopyHDRShaderName);

            if (settings.UberShader == uber
                && settings.ColorScaleBiasShader == colorScaleBias
                && settings.BlitCopyHDRShader == blitCopyHDR)
            {
                return false;
            }

            settings.UberShader = uber;
            settings.ColorScaleBiasShader = colorScaleBias;
            settings.BlitCopyHDRShader = blitCopyHDR;
            EditorUtility.SetDirty(settings);
            return true;
        }

        /// <summary>
        /// Appends the asset unless the list already contains it, leaving existing entries in order.
        /// </summary>
        /// <returns><see langword="true"/> if the asset was appended.</returns>
        internal static bool TryAppendMissing(Object[] assets, Object asset, out Object[] updatedAssets)
        {
            if (Array.IndexOf(assets, asset) >= 0)
            {
                updatedAssets = assets;
                return false;
            }

            updatedAssets = new Object[assets.Length + 1];
            assets.CopyTo(updatedAssets, 0);
            updatedAssets[assets.Length] = asset;
            return true;
        }

        /// <summary>
        /// Creates every missing folder on an asset folder path, such as <c>Assets/A/B</c>.
        /// </summary>
        internal static void CreateFolders(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            CreateFolders(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// Deletes a folder an asset was moved or deleted from, and its parents up to and including the
        /// <c>Resources</c> folder, while they are empty.
        /// </summary>
        internal static void DeleteEmptiedResourcesFolders(string folder)
        {
            while (RuntimeSettingsLocations.IsInResourcesFolder(folder) && IsEmptyOnDisk(folder))
            {
                var isResourcesFolder = Path.GetFileName(folder) == "Resources";
                AssetDatabase.DeleteAsset(folder);
                if (isResourcesFolder)
                    return;

                folder = GetFolder(folder);
            }
        }

        internal static string GetFolder(string assetPath) => assetPath.Substring(0, assetPath.LastIndexOf('/'));

        static CompositionLayersRuntimeSettings Create(RuntimeSettingsLocations locations)
        {
            CreateFolders(locations.DefaultFolder);
            var settings = ScriptableObject.CreateInstance<CompositionLayersRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, GetFreeDefaultAssetPath(locations));
            return settings;
        }

        // Moving keeps the asset's GUID, so references to it, such as an existing Preloaded Assets entry, stay valid.
        static bool TryMoveToDefaultFolder(
            CompositionLayersRuntimeSettings settings, RuntimeSettingsLocations locations)
        {
            var sourcePath = AssetDatabase.GetAssetPath(settings);
            CreateFolders(locations.DefaultFolder);
            var error = AssetDatabase.MoveAsset(sourcePath, GetFreeDefaultAssetPath(locations));
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"XR Composition Layers could not move {sourcePath} to {locations.DefaultFolder}: {error}");
                return false;
            }

            DeleteEmptiedResourcesFolders(GetFolder(sourcePath));
            return true;
        }

        // Creating or moving an asset onto a taken path replaces or fails on what is there, so never use one.
        static string GetFreeDefaultAssetPath(RuntimeSettingsLocations locations)
        {
            return AssetDatabase.GenerateUniqueAssetPath(locations.DefaultAssetPath);
        }

        static bool TryRemoveOtherSettings(
            Object[] preloadedAssets, CompositionLayersRuntimeSettings settings, out Object[] updatedPreloadedAssets)
        {
            if (!ContainsOtherSettings(preloadedAssets, settings))
            {
                updatedPreloadedAssets = preloadedAssets;
                return false;
            }

            var keptAssets = new List<Object>(preloadedAssets.Length);
            foreach (var asset in preloadedAssets)
            {
                if (!IsOtherSettings(asset, settings))
                    keptAssets.Add(asset);
            }

            updatedPreloadedAssets = keptAssets.ToArray();
            return true;
        }

        static bool IsOnlySettingsPreloaded(Object[] preloadedAssets, CompositionLayersRuntimeSettings settings)
        {
            return Array.IndexOf(preloadedAssets, settings) >= 0 && !ContainsOtherSettings(preloadedAssets, settings);
        }

        static bool ContainsOtherSettings(Object[] preloadedAssets, CompositionLayersRuntimeSettings settings)
        {
            foreach (var asset in preloadedAssets)
            {
                if (IsOtherSettings(asset, settings))
                    return true;
            }

            return false;
        }

        static bool IsOtherSettings(Object asset, CompositionLayersRuntimeSettings settings)
        {
            return asset is CompositionLayersRuntimeSettings preloadedSettings
                && preloadedSettings != null
                && preloadedSettings != settings;
        }

        static bool HasPackageShaders(CompositionLayersRuntimeSettings settings)
        {
            return settings.UberShader == Shader.Find(CompositionLayerConstants.UberShaderName)
                && settings.ColorScaleBiasShader == Shader.Find(CompositionLayerConstants.ColorScaleBiasShaderName)
                && settings.BlitCopyHDRShader == Shader.Find(CompositionLayerConstants.BlitCopyHDRShaderName)
                && settings.UberShader != null
                && settings.ColorScaleBiasShader != null
                && settings.BlitCopyHDRShader != null;
        }

        // Checks the file system rather than the asset database, which doesn't list unimported files such as .gitkeep.
        static bool IsEmptyOnDisk(string folder)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
            {
                if (Path.GetFileName(entry) != k_MacOSFolderMetadataFileName)
                    return false;
            }

            return true;
        }

        static Shader FindShaderOrFailBuild(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new BuildFailedException(
                    $"Shader '{shaderName}' is missing, so emulated composition layers would not render.");

            return shader;
        }
    }
}
