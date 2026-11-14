using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.CompositionLayers.Editor.ProjectValidation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// The validation types are obsolete, and these tests cover them.
#pragma warning disable CS0618

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class LeftoverPreferencesRuleTests
    {
        const string k_TestFolder = "Assets/LeftoverPreferencesRuleTests";
        const string k_DefaultRelativePath = "CompositionLayers/UserSettings/CompositionLayersPreferences.asset";
        const string k_MovedRelativePath = "Moved/RenamedPreferences.asset";
        const string k_PreferencesScriptGuid = "c66150246d824833a8257ecb5aaa1d11";

        // Matches what the removed preferences class serialized, with both settings changed from their defaults.
        const string k_LeftoverAssetYaml =
            "%YAML 1.1\n" +
            "%TAG !u! tag:unity3d.com,2011:\n" +
            "--- !u!114 &11400000\n" +
            "MonoBehaviour:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_CorrespondingSourceObject: {fileID: 0}\n" +
            "  m_PrefabInstance: {fileID: 0}\n" +
            "  m_PrefabAsset: {fileID: 0}\n" +
            "  m_GameObject: {fileID: 0}\n" +
            "  m_Enabled: 1\n" +
            "  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: " + k_PreferencesScriptGuid + ", type: 3}\n" +
            "  m_Name: CompositionLayersPreferences\n" +
            "  m_EditorClassIdentifier:\n" +
            "  m_DisplayAnglesAs: 1\n" +
            "  m_EmulationInScene: 0\n";

        CompositionLayersPreferencesCheck m_Check;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(k_TestFolder);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(k_TestFolder));
            m_Check = new CompositionLayersPreferencesCheck(k_TestFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_TestFolder);
        }

        [TestCase(k_DefaultRelativePath)]
        [TestCase(k_MovedRelativePath)]
        public void Rule_FailsWhenAssetExists(string relativePath)
        {
            var rule = CompositionLayerBuildValidationRuleFactory.CreateLeftoverPreferencesRule(m_Check);
            WriteLeftoverAsset(relativePath);

            Assert.IsFalse(rule.CheckPredicate());
        }

        [Test]
        public void Rule_IsAWarning()
        {
            var expected = CompositionLayerBuildValidationRuleFactory.CreateLeftoverPreferencesRule(m_Check);

            foreach (var buildTargetGroup in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var registered = FindRegisteredRule(buildTargetGroup, expected.Message);

                Assert.IsNotNull(registered, $"Rule is not registered for {buildTargetGroup}.");
                Assert.IsFalse(registered.Error);
                Assert.IsFalse(registered.requiresActiveManager);
                StringAssert.Contains("upgrade-guide-7.0.html", registered.HelpLink);
            }
        }

        [Test]
        public void Fix_DeletesEveryCopyAndLogsPaths()
        {
            var rule = CompositionLayerBuildValidationRuleFactory.CreateLeftoverPreferencesRule(m_Check);
            var defaultPath = WriteLeftoverAsset(k_DefaultRelativePath);
            var movedPath = WriteLeftoverAsset(k_MovedRelativePath);
            LogAssert.Expect(
                LogType.Log,
                new Regex($"(?s)(?=.*{Regex.Escape(defaultPath)})(?=.*{Regex.Escape(movedPath)})"));

            rule.FixIt();

            Assert.IsFalse(File.Exists(defaultPath));
            Assert.IsFalse(File.Exists(movedPath));
            Assert.IsTrue(rule.CheckPredicate());
        }

        [Test]
        public void Check_RefreshesAfterImportAndDelete()
        {
            Assert.IsFalse(m_Check.HasLeftoverAssets());

            var path = WriteLeftoverAsset(k_DefaultRelativePath);
            Assert.IsTrue(m_Check.HasLeftoverAssets(), "Not refreshed after import.");

            AssetDatabase.DeleteAsset(path);
            Assert.IsFalse(m_Check.HasLeftoverAssets(), "Not refreshed after delete.");
        }

        // EditorScriptableSettings creates and saves a missing asset, so the shell must not derive from it.
        [Test]
        public void Shell_IsPlainScriptableObject()
        {
            Assert.AreEqual(typeof(ScriptableObject), typeof(CompositionLayersPreferences).BaseType);
        }

        [Test]
        public void Inspector_ShowsDeprecationWarningWithoutButtons()
        {
            var path = WriteLeftoverAsset(k_DefaultRelativePath);
            var inspector = UnityEditor.Editor.CreateEditor(AssetDatabase.LoadMainAssetAtPath(path));
            try
            {
                var root = inspector.CreateInspectorGUI();

                Assert.IsNotNull(root, "The asset has no custom Inspector.");
                var helpBox = root as HelpBox ?? root.Q<HelpBox>();
                Assert.IsNotNull(helpBox);
                Assert.AreEqual(HelpBoxMessageType.Warning, helpBox.messageType);
                StringAssert.Contains("7.0.0-pre.1", helpBox.text);
                Assert.IsNull(root.Q<Button>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inspector);
            }
        }

        static string WriteLeftoverAsset(string relativePath)
        {
            var path = k_TestFolder + "/" + relativePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, k_LeftoverAssetYaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        static CompositionLayerBuildValidationRule FindRegisteredRule(BuildTargetGroup buildTargetGroup, string message)
        {
            var issues = new List<CompositionLayerBuildValidationRule>();
            CompositionLayerProjectValidation.GetAllValidationIssues(issues, buildTargetGroup);
            foreach (var issue in issues)
            {
                if (issue.Message == message)
                    return issue;
            }

            return null;
        }
    }
}
