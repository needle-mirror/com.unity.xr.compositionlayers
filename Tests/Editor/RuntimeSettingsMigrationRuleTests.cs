using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.CompositionLayers.Editor.ProjectValidation;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
#pragma warning disable CS0618 // The validation types are obsolete for use outside the package.
    class RuntimeSettingsMigrationRuleTests : RuntimeSettingsTestBase
    {
        const string k_NoAsset = "no asset";
        const string k_NewOnly = "new asset only";
        const string k_OldOnly = "old asset only";
        const string k_OldPathCase = "old Resources path";
        const string k_OtherFolderAssetPath = k_TestFolder + "/Moved/Moved.asset";
        const string k_OtherResourcesAssetPath = k_TestFolder + "/Other/Resources/Other.asset";

        RuntimeSettingsValidation m_Validation;
        Object[] m_PreloadedAssets;

        [SetUp]
        public void CreateValidation()
        {
            m_PreloadedAssets = Array.Empty<Object>();
            m_Validation = new RuntimeSettingsValidation(
                Locations, () => m_PreloadedAssets, assets => m_PreloadedAssets = assets);
        }

        [TestCase(k_NoAsset)]
        [TestCase(k_NewOnly)]
        [TestCase(k_OldOnly)]
        public void OldCopyRule_Passes(string assetCase)
        {
            if (assetCase == k_NewOnly)
                CreateSettingsAsset(Locations.DefaultAssetPath);
            else if (assetCase == k_OldOnly)
                CreateSettingsAsset(Locations.LegacyAssetPath);

            var rule = RuntimeSettingsValidation.CreateOldCopyRule(m_Validation);

            Assert.IsTrue(rule.CheckPredicate());
        }

        [TestCase(k_OldPathCase)]
        [TestCase(k_OtherResourcesAssetPath)]
        public void OldCopyRule_FailsWhenNewAndOldExist(string oldAssetCase)
        {
            CreateSettingsAsset(Locations.DefaultAssetPath);
            CreateSettingsAsset(oldAssetCase == k_OldPathCase ? Locations.LegacyAssetPath : oldAssetCase);

            var rule = RuntimeSettingsValidation.CreateOldCopyRule(m_Validation);

            Assert.IsFalse(rule.CheckPredicate());
        }

        [Test]
        public void OldCopyFix_DeletesOldCopyAndEmptiedResources_KeepsNew()
        {
            CreateSettingsAsset(Locations.LegacyAssetPath);
            CreateSettingsAsset(Locations.DefaultAssetPath);
            var newGuid = AssetDatabase.AssetPathToGUID(Locations.DefaultAssetPath);
            var rule = RuntimeSettingsValidation.CreateOldCopyRule(m_Validation);

            rule.FixIt();

            Assert.IsFalse(AssetDatabase.AssetPathExists(Locations.LegacyAssetPath));
            Assert.IsFalse(AssetDatabase.IsValidFolder(Locations.LegacyFolder));
            Assert.AreEqual(Locations.DefaultAssetPath, AssetDatabase.GUIDToAssetPath(newGuid));
            Assert.IsTrue(rule.CheckPredicate());
        }

        [Test]
        public void OldCopyFix_RemovesOldCopyFromPreloadedAssets()
        {
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            CreateSettingsAsset(Locations.DefaultAssetPath);
            var otherPreloadedAsset = CreateSettingsAsset(k_OtherFolderAssetPath);
            m_PreloadedAssets = new Object[] { oldSettings, otherPreloadedAsset };
            var rule = RuntimeSettingsValidation.CreateOldCopyRule(m_Validation);

            rule.FixIt();

            CollectionAssert.AreEqual(new Object[] { otherPreloadedAsset }, m_PreloadedAssets);
        }

        [Test]
        public void Check_RefreshesAfterAssetChanges()
        {
            Assert.IsFalse(m_Validation.HasOldAsset());
            Assert.IsFalse(m_Validation.HasNewAsset());

            CreateSettingsAsset(Locations.LegacyAssetPath);
            Assert.IsTrue(m_Validation.HasOldAsset(), "Not refreshed after create.");

            CreateFoldersFor(k_OtherFolderAssetPath);
            Assert.IsEmpty(AssetDatabase.MoveAsset(Locations.LegacyAssetPath, k_OtherFolderAssetPath));
            Assert.IsFalse(m_Validation.HasOldAsset(), "Not refreshed after move.");
            Assert.IsTrue(m_Validation.HasNewAsset(), "New asset not refreshed after move.");
        }
    }
#pragma warning restore CS0618
}
