using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.CompositionLayers.Editor.ProjectValidation;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
#pragma warning disable CS0618 // The validation types are obsolete for use outside the package.
    class RuntimeSettingsSetupRuleTests : RuntimeSettingsTestBase
    {
        const string k_NoAsset = "no asset";
        const string k_OldAssetOnly = "old asset only";
        const string k_ShadersMissing = "shaders missing";
        const string k_NotPreloaded = "not preloaded";
        const string k_OtherSettingsPreloaded = "another settings asset preloaded";
        const string k_OtherFolderAssetPath = k_TestFolder + "/Moved/Moved.asset";
        const string k_MigrateRule = "migrate";
        const string k_MissingRule = "missing";
        const string k_NotPreloadedRule = "not preloaded";
        const string k_EnableSplashScreenProperty = "m_EnableSplashScreen";
        const string k_EmulationInStandaloneProperty = "m_EmulationInStandalone";

        static readonly string[] k_NotSetUpCases =
        {
            k_NoAsset, k_OldAssetOnly, k_ShadersMissing, k_NotPreloaded, k_OtherSettingsPreloaded,
        };

        RuntimeSettingsValidation m_Validation;
        Object[] m_PreloadedAssets;

        [SetUp]
        public void CreateValidation()
        {
            m_PreloadedAssets = Array.Empty<Object>();
            m_Validation = new RuntimeSettingsValidation(
                Locations, () => m_PreloadedAssets, assets => m_PreloadedAssets = assets);
        }

        [TestCaseSource(nameof(k_NotSetUpCases))]
        public void SetupRules_OnlyTheMatchingRuleFails(string notSetUpCase)
        {
            ArrangeNotSetUp(notSetUpCase);
            var expectedRule = GetExpectedRuleName(notSetUpCase);

            foreach (var (ruleName, rule) in CreateSetupRules())
            {
                Assert.AreEqual(ruleName != expectedRule, rule.CheckPredicate(), $"{ruleName} rule");
            }
        }

        [TestCaseSource(nameof(k_NotSetUpCases))]
        public void SetupRules_FixSetsUp(string notSetUpCase)
        {
            ArrangeNotSetUp(notSetUpCase);
            var rules = CreateSetupRules();

            rules[GetExpectedRuleName(notSetUpCase)].FixIt();

            foreach (var (ruleName, rule) in rules)
            {
                Assert.IsTrue(rule.CheckPredicate(), $"{ruleName} rule");
            }
        }

        [TestCase(true, k_EnableSplashScreenProperty)]
        [TestCase(true, k_EmulationInStandaloneProperty)]
        [TestCase(false, k_EnableSplashScreenProperty)]
        public void Build_SettingsThatChangePlayerNotPreloaded_FailsAndChangesNothing(
            bool inResourcesFolder, string enabledProperty)
        {
            var assetPath = inResourcesFolder ? Locations.LegacyAssetPath : Locations.DefaultAssetPath;
            var settings = CreateSettingsAsset(assetPath);
            EnableSetting(settings, enabledProperty);
            var filesBefore = GetFilesInTestFolder();

            Assert.Throws<BuildFailedException>(
                () => RuntimeSettingsBuildProcessor.Validate(Locations, m_PreloadedAssets));

            CollectionAssert.AreEquivalent(filesBefore, GetFilesInTestFolder());
            CollectionAssert.IsEmpty(m_PreloadedAssets);
        }

        [Test]
        public void Build_ConflictingPreloadedSettings_FailsAndChangesNothing()
        {
            ArrangeNotSetUp(k_OtherSettingsPreloaded);
            EnableSetting((CompositionLayersRuntimeSettings)m_PreloadedAssets[1], k_EmulationInStandaloneProperty);
            var filesBefore = GetFilesInTestFolder();
            var preloadedAssetsBefore = (Object[])m_PreloadedAssets.Clone();

            Assert.Throws<BuildFailedException>(
                () => RuntimeSettingsBuildProcessor.Validate(Locations, m_PreloadedAssets));

            CollectionAssert.AreEquivalent(filesBefore, GetFilesInTestFolder());
            CollectionAssert.AreEqual(preloadedAssetsBefore, m_PreloadedAssets);
        }

        // The build check and the rules share one test of what "set up" means, so one case shows the build warns.
        // An untouched old asset, as 6.x created for any project that read the settings, warns too.
        [TestCase(k_NotPreloaded)]
        [TestCase(k_OldAssetOnly)]
        [TestCase(k_OtherSettingsPreloaded)]
        public void Build_NotSetUp_WarnsAndChangesNothing(string notSetUpCase)
        {
            ArrangeNotSetUp(notSetUpCase);
            var filesBefore = GetFilesInTestFolder();
            var preloadedAssetsBefore = (Object[])m_PreloadedAssets.Clone();
            LogAssert.Expect(LogType.Warning, RuntimeSettingsMessages.k_BuildNotSetUp);

            RuntimeSettingsBuildProcessor.Validate(Locations, m_PreloadedAssets);

            CollectionAssert.AreEquivalent(filesBefore, GetFilesInTestFolder());
            CollectionAssert.AreEqual(preloadedAssetsBefore, m_PreloadedAssets);
        }

        [Test]
        public void Rules_AreRegisteredForEveryBuildTarget()
        {
            // The registered rules check the project's default locations, so their messages name those paths.
            var projectValidation = new RuntimeSettingsValidation(
                RuntimeSettingsLocations.Default, Array.Empty<Object>, _ => { });
            var expectedRules = new[]
            {
                RuntimeSettingsValidation.CreateMigrateRule(projectValidation),
                RuntimeSettingsValidation.CreateMissingRule(projectValidation),
                RuntimeSettingsValidation.CreateNotPreloadedRule(projectValidation),
                RuntimeSettingsValidation.CreateOldCopyRule(projectValidation),
            };
            var issues = new List<CompositionLayerBuildValidationRule>();

            foreach (var buildTargetGroup in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                CompositionLayerProjectValidation.GetAllValidationIssues(issues, buildTargetGroup);
                foreach (var expected in expectedRules)
                {
                    Assert.IsNotNull(
                        issues.Find(issue => issue.Message == expected.Message),
                        $"\"{expected.Message}\" is not registered for {buildTargetGroup}.");
                }
            }
        }

        Dictionary<string, CompositionLayerBuildValidationRule> CreateSetupRules()
        {
            return new Dictionary<string, CompositionLayerBuildValidationRule>
            {
                { k_MigrateRule, RuntimeSettingsValidation.CreateMigrateRule(m_Validation) },
                { k_MissingRule, RuntimeSettingsValidation.CreateMissingRule(m_Validation) },
                { k_NotPreloadedRule, RuntimeSettingsValidation.CreateNotPreloadedRule(m_Validation) },
            };
        }

        static string GetExpectedRuleName(string notSetUpCase)
        {
            return notSetUpCase switch
            {
                k_NoAsset => k_MissingRule,
                k_OldAssetOnly => k_MigrateRule,
                _ => k_NotPreloadedRule,
            };
        }

        static void EnableSetting(CompositionLayersRuntimeSettings settings, string propertyPath)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty(propertyPath).boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        void ArrangeNotSetUp(string notSetUpCase)
        {
            switch (notSetUpCase)
            {
                case k_NoAsset:
                    return;

                // Complete apart from being in Resources, so only its location fails the rule.
                case k_OldAssetOnly:
                {
                    var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
                    RuntimeSettingsSetup.AssignShaders(oldSettings);
                    m_PreloadedAssets = new Object[] { oldSettings };
                    return;
                }

                case k_ShadersMissing:
                    m_PreloadedAssets = new Object[] { CreateSettingsAsset(Locations.DefaultAssetPath) };
                    return;

                case k_NotPreloaded:
                    RuntimeSettingsSetup.AssignShaders(CreateSettingsAsset(Locations.DefaultAssetPath));
                    return;

                case k_OtherSettingsPreloaded:
                {
                    var settings = CreateSettingsAsset(Locations.DefaultAssetPath);
                    RuntimeSettingsSetup.AssignShaders(settings);
                    m_PreloadedAssets = new Object[] { settings, CreateSettingsAsset(k_OtherFolderAssetPath) };
                    return;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(notSetUpCase), notSetUpCase, null);
            }
        }
    }
#pragma warning restore CS0618
}
