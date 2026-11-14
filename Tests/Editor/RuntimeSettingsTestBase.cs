using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    /// <summary>
    /// Runs each test against settings locations in a temporary folder. After every test, including one that failed,
    /// it deletes the folder and any settings assets or package folders the test added to the project, restores
    /// Preloaded Assets and the Build Settings scenes, and then fails the test if it changed any of them or dirtied
    /// the open scene.
    /// </summary>
    abstract class RuntimeSettingsTestBase
    {
        protected const string k_TestFolder = "Assets/RuntimeSettingsTests";
        const string k_SettingsTypeFilter = "t:" + nameof(CompositionLayersRuntimeSettings);

        static readonly string[] k_ProjectSearchFolders = { "Assets" };

        // The folders the package creates in a project. Only deleted when they didn't exist before the fixture ran.
        static readonly string[] k_PackageFolders =
        {
            "Assets/CompositionLayers", SplashSceneSetup.k_DefaultFolder,
        };

        HashSet<string> m_ProjectSettingsGuids;
        List<string> m_MissingPackageFolders;
        Object[] m_PreloadedAssets;
        EditorBuildSettingsScene[] m_BuildScenes;
        bool m_WasActiveSceneDirty;

        protected RuntimeSettingsLocations Locations { get; } = new(
            k_TestFolder + "/XR/Settings", k_TestFolder + "/CompositionLayers/UserSettings/Resources", k_TestFolder);

        [OneTimeSetUp]
        public void SnapshotProject()
        {
            m_ProjectSettingsGuids = new HashSet<string>(FindProjectSettingsAssets());
            m_MissingPackageFolders = new List<string>();
            foreach (var folder in k_PackageFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    m_MissingPackageFolders.Add(folder);
            }

            m_PreloadedAssets = PlayerSettings.GetPreloadedAssets();
            m_BuildScenes = EditorBuildSettings.scenes;
            m_WasActiveSceneDirty = SceneManager.GetActiveScene().isDirty;
        }

        [SetUp]
        public void CreateTestFolder()
        {
            AssetDatabase.DeleteAsset(k_TestFolder);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(k_TestFolder));
        }

        // NUnit runs this after the derived fixtures' TearDown methods, including when a test fails or throws.
        [TearDown]
        public void RestoreProject()
        {
            RuntimeSettingsLookup.Override = null;
            AssetDatabase.DeleteAsset(k_TestFolder);

            var addedSettingsPaths = DeleteAddedSettingsAssets();
            var addedFolders = DeleteAddedPackageFolders();
            var preloadedAssetsChanged = !AreEqual(m_PreloadedAssets, PlayerSettings.GetPreloadedAssets());
            if (preloadedAssetsChanged)
                PlayerSettings.SetPreloadedAssets(m_PreloadedAssets);

            var buildScenesChanged = !AreEqual(m_BuildScenes, EditorBuildSettings.scenes);
            if (buildScenesChanged)
                EditorBuildSettings.scenes = m_BuildScenes;

            var changes = new List<string>();
            if (addedSettingsPaths.Count > 0)
                changes.Add("added settings assets " + string.Join(", ", addedSettingsPaths));

            if (addedFolders.Count > 0)
                changes.Add("added folders " + string.Join(", ", addedFolders));

            if (preloadedAssetsChanged)
                changes.Add("changed Preloaded Assets");

            if (buildScenesChanged)
                changes.Add("changed the Build Settings scenes");

            if (!m_WasActiveSceneDirty && SceneManager.GetActiveScene().isDirty)
                changes.Add("dirtied the open scene");

            CollectionAssert.IsEmpty(
                changes, "The test changed the project. Everything but the open scene is restored.");
        }

        /// <summary>
        /// Creates every missing folder in an asset path's directory.
        /// </summary>
        protected static void CreateFoldersFor(string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;

            CreateFoldersFor(folder);
            AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        }

        protected static CompositionLayersRuntimeSettings CreateSettingsAsset(string assetPath)
        {
            CreateFoldersFor(assetPath);
            var settings = ScriptableObject.CreateInstance<CompositionLayersRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            return settings;
        }

        protected static void WriteTextAsset(string assetPath, string contents)
        {
            CreateFoldersFor(assetPath);
            File.WriteAllText(assetPath, contents);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        protected static string[] FindProjectSettingsAssets()
        {
            return AssetDatabase.FindAssets(k_SettingsTypeFilter, k_ProjectSearchFolders);
        }

        protected static int CountProjectSettingsAssets() => FindProjectSettingsAssets().Length;

        protected static string GetPathOrEmpty(Object asset) => asset == null ? "" : AssetDatabase.GetAssetPath(asset);

        protected static void AssertPackageShadersAssigned(CompositionLayersRuntimeSettings settings)
        {
            Assert.AreSame(Shader.Find(CompositionLayerConstants.UberShaderName), settings.UberShader);
            Assert.AreSame(
                Shader.Find(CompositionLayerConstants.ColorScaleBiasShaderName), settings.ColorScaleBiasShader);
            Assert.AreSame(Shader.Find(CompositionLayerConstants.BlitCopyHDRShaderName), settings.BlitCopyHDRShader);
        }

        protected static void SetSplashDuration(CompositionLayersRuntimeSettings settings, float duration)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_SplashDuration").floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        protected static float GetSplashDuration(CompositionLayersRuntimeSettings settings)
        {
            return new SerializedObject(settings).FindProperty("m_SplashDuration").floatValue;
        }

        protected static string[] GetFilesInTestFolder()
        {
            return Directory.GetFiles(k_TestFolder, "*", SearchOption.AllDirectories);
        }

        List<string> DeleteAddedSettingsAssets()
        {
            var addedPaths = new List<string>();
            foreach (var guid in FindProjectSettingsAssets())
            {
                if (m_ProjectSettingsGuids.Contains(guid))
                    continue;

                var path = AssetDatabase.GUIDToAssetPath(guid);
                addedPaths.Add(path);
                AssetDatabase.DeleteAsset(path);
            }

            return addedPaths;
        }

        List<string> DeleteAddedPackageFolders()
        {
            var addedFolders = new List<string>();
            foreach (var folder in m_MissingPackageFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                addedFolders.Add(folder);
                AssetDatabase.DeleteAsset(folder);
            }

            return addedFolders;
        }

        static bool AreEqual(Object[] expected, Object[] actual)
        {
            if (expected.Length != actual.Length)
                return false;

            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                    return false;
            }

            return true;
        }

        static bool AreEqual(EditorBuildSettingsScene[] expected, EditorBuildSettingsScene[] actual)
        {
            if (expected.Length != actual.Length)
                return false;

            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i].path != actual[i].path || expected[i].enabled != actual[i].enabled)
                    return false;
            }

            return true;
        }
    }
}
