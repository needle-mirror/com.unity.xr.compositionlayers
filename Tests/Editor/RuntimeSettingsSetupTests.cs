using System;
using System.IO;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RuntimeSettingsSetupTests : RuntimeSettingsTestBase
    {
        const string k_CreateCase = "create";
        const string k_MoveCase = "move";
        const string k_OtherFolderAssetPath = k_TestFolder + "/Moved/Moved.asset";
        const float k_ChangedSplashDuration = 7f;

        [Test]
        public void SetUp_NoAsset_CreatesAssignsShadersAndPreloads()
        {
            var changed = RuntimeSettingsSetup.SetUp(
                Locations, Array.Empty<Object>(), out var settings, out var preloadedAssets);

            Assert.IsTrue(changed);
            Assert.AreEqual(Locations.DefaultAssetPath, GetPathOrEmpty(settings));
            AssertPackageShadersAssigned(settings);
            CollectionAssert.AreEqual(new Object[] { settings }, preloadedAssets);
        }

        [Test]
        public void SetUp_OldAssetOnly_MovesItKeepingGuidAndValues()
        {
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            SetSplashDuration(oldSettings, k_ChangedSplashDuration);
            var guid = AssetDatabase.AssetPathToGUID(Locations.LegacyAssetPath);

            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out var settings, out var preloadedAssets);

            Assert.AreSame(oldSettings, settings);
            Assert.AreEqual(Locations.DefaultAssetPath, AssetDatabase.GUIDToAssetPath(guid));
            Assert.AreEqual(k_ChangedSplashDuration, GetSplashDuration(settings));
            AssertPackageShadersAssigned(settings);
            CollectionAssert.AreEqual(new Object[] { settings }, preloadedAssets);
            Assert.IsFalse(AssetDatabase.IsValidFolder(Locations.LegacyFolder));
        }

        [Test]
        public void SetUp_OldAsset_KeepsNonEmptyResources()
        {
            CreateSettingsAsset(Locations.LegacyAssetPath);
            var otherFilePath = Locations.LegacyFolder + "/Other.txt";
            WriteTextAsset(otherFilePath, "kept");
            var guid = AssetDatabase.AssetPathToGUID(Locations.LegacyAssetPath);

            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out _, out _);

            Assert.AreEqual(Locations.DefaultAssetPath, AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsTrue(File.Exists(otherFilePath));
        }

        [Test]
        public void SetUp_OldAsset_KeepsResourcesWithUnimportedFile()
        {
            CreateSettingsAsset(Locations.LegacyAssetPath);
            var hiddenFilePath = Locations.LegacyFolder + "/.gitkeep";
            File.WriteAllText(hiddenFilePath, "");

            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out _, out _);

            Assert.IsTrue(File.Exists(hiddenFilePath));
        }

        [Test]
        public void SetUp_NewAndOldAssets_UsesNewAndLeavesOldAlone()
        {
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            var newSettings = CreateSettingsAsset(Locations.DefaultAssetPath);

            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out var settings, out _);

            Assert.AreSame(newSettings, settings);
            Assert.AreEqual(Locations.LegacyAssetPath, GetPathOrEmpty(oldSettings));
        }

        [TestCase(k_CreateCase)]
        [TestCase(k_MoveCase)]
        public void SetUp_DefaultPathTaken_UsesUniquePathAndLeavesFile(string setUpCase)
        {
            if (setUpCase == k_MoveCase)
                CreateSettingsAsset(Locations.LegacyAssetPath);

            CreateFoldersFor(Locations.DefaultAssetPath);
            AssetDatabase.CreateAsset(new Texture2D(4, 4), Locations.DefaultAssetPath);
            var unrelatedBytes = File.ReadAllBytes(Locations.DefaultAssetPath);

            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out var settings, out _);

            var expectedPath = Locations.DefaultFolder + "/CompositionLayersRuntimeSettings 1.asset";
            Assert.AreEqual(expectedPath, GetPathOrEmpty(settings));
            CollectionAssert.AreEqual(unrelatedBytes, File.ReadAllBytes(Locations.DefaultAssetPath));
        }

        [Test]
        public void SetUp_RemovesOtherSettingsAssetsFromPreloadedAssets()
        {
            var newSettings = CreateSettingsAsset(Locations.DefaultAssetPath);
            var otherNewSettings = CreateSettingsAsset(k_OtherFolderAssetPath);
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            var otherAsset = Shader.Find(CompositionLayerConstants.UberShaderName);
            var preloadedAssets = new Object[] { newSettings, otherAsset, oldSettings, otherNewSettings, null };

            RuntimeSettingsSetup.SetUp(Locations, preloadedAssets, out var settings, out var updatedPreloadedAssets);

            Assert.AreSame(newSettings, settings);
            CollectionAssert.AreEqual(new Object[] { newSettings, otherAsset, null }, updatedPreloadedAssets);
        }

        [Test]
        public void SetUp_SecondRun_ChangesNothing()
        {
            RuntimeSettingsSetup.SetUp(Locations, Array.Empty<Object>(), out _, out var firstPreloadedAssets);
            var assetBytes = File.ReadAllBytes(Locations.DefaultAssetPath);

            var changed = RuntimeSettingsSetup.SetUp(
                Locations, firstPreloadedAssets, out _, out var secondPreloadedAssets);

            Assert.IsFalse(changed);
            Assert.AreSame(firstPreloadedAssets, secondPreloadedAssets);
            CollectionAssert.AreEqual(assetBytes, File.ReadAllBytes(Locations.DefaultAssetPath));
        }

    }
}
