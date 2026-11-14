using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RuntimeSettingsLookupTests : RuntimeSettingsTestBase
    {
        const string k_PreloadedLocation = "new asset in Preloaded Assets";
        const string k_DefaultLocation = "default path";
        const string k_ByTypeLocation = "by type";
        const string k_LegacyLocation = "old Resources path";
        const string k_OtherResourcesLocation = "old asset in another Resources folder";

        static readonly string[] k_LocationsInLookupOrder =
        {
            k_PreloadedLocation, k_DefaultLocation, k_ByTypeLocation, k_LegacyLocation, k_OtherResourcesLocation,
        };

        [TestCaseSource(nameof(k_LocationsInLookupOrder))]
        public void Find_ReturnsFirstLocationInOrder(string expectedLocation)
        {
            var preloadedAssets = Array.Empty<Object>();
            CompositionLayersRuntimeSettings expected = null;
            var isAtOrAfterExpected = false;
            foreach (var location in k_LocationsInLookupOrder)
            {
                isAtOrAfterExpected |= location == expectedLocation;
                if (!isAtOrAfterExpected)
                    continue;

                var settings = CreateSettingsAsset(GetPathFor(location));
                expected ??= settings;
                if (location == k_PreloadedLocation)
                    preloadedAssets = new Object[] { settings };
            }

            var found = RuntimeSettingsLookup.Find(Locations, preloadedAssets);

            Assert.AreSame(
                expected, found, $"Expected the asset at the {expectedLocation}, got {GetPathOrEmpty(found)}.");
        }

        [Test]
        public void Find_PrefersNewAssetOverOldAssetInPreloadedAssets()
        {
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            var newSettings = CreateSettingsAsset(GetPathFor(k_ByTypeLocation));

            var found = RuntimeSettingsLookup.Find(Locations, new Object[] { oldSettings });

            Assert.AreSame(newSettings, found, $"Got {GetPathOrEmpty(found)}.");
        }

        [Test]
        public void Find_NoAsset_ReturnsDefaultsAndCreatesNoFile()
        {
            var settings = RuntimeSettingsLookup.Find(Locations, Array.Empty<Object>());

            Assert.IsNotNull(settings);
            Assert.AreEqual("", AssetDatabase.GetAssetPath(settings));
            Assert.IsTrue((settings.hideFlags & HideFlags.DontSave) == HideFlags.DontSave);
            var serialized = new SerializedObject(settings);
            Assert.IsFalse(serialized.FindProperty("m_EnableSplashScreen").boolValue);
            Assert.IsFalse(serialized.FindProperty("m_EmulationInStandalone").boolValue);
            Assert.AreEqual(3f, serialized.FindProperty("m_SplashDuration").floatValue);
            CollectionAssert.IsEmpty(GetFilesInTestFolder());
        }

        string GetPathFor(string location)
        {
            return location switch
            {
                k_PreloadedLocation => k_TestFolder + "/Preloaded/Preloaded.asset",
                k_DefaultLocation => Locations.DefaultAssetPath,
                k_ByTypeLocation => k_TestFolder + "/Moved/Moved.asset",
                k_LegacyLocation => Locations.LegacyAssetPath,
                k_OtherResourcesLocation => k_TestFolder + "/Other/Resources/Other.asset",
                _ => throw new ArgumentOutOfRangeException(nameof(location), location, null),
            };
        }
    }
}
