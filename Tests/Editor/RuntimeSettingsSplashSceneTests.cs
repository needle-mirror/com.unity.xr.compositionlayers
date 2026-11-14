using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RuntimeSettingsSplashSceneTests : RuntimeSettingsTestBase
    {
        const string k_OtherScenePath = "Assets/Scenes/Other.unity";
        const string k_SplashToken = "Splash";
        const string k_OtherToken = "Other";

        static string SplashScenePath => k_TestFolder + "/" + SplashSceneSetup.k_SceneFileName;

        [TestCase(false, TestName = "EnablingSplash_CopiesTemplateAndInsertsFirst(no copy yet)")]
        [TestCase(true, TestName = "EnablingSplash_CopiesTemplateAndInsertsFirst(copy already present)")]
        public void EnablingSplash_CopiesTemplateAndInsertsFirst(bool copyExists)
        {
            var existingGuid = "";
            if (copyExists)
            {
                Assert.IsTrue(AssetDatabase.CopyAsset(SplashSceneSetup.k_TemplatePath, SplashScenePath));
                existingGuid = AssetDatabase.AssetPathToGUID(SplashScenePath);
            }

            var scenes = SplashSceneSetup.UpdateScenes(true, BuildScenes(k_OtherToken), k_TestFolder);

            AssertScenePaths(new[] { SplashScenePath, k_OtherScenePath }, scenes);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(SplashScenePath));
            if (copyExists)
                Assert.AreEqual(existingGuid, AssetDatabase.AssetPathToGUID(SplashScenePath), "The copy was replaced.");
        }

        [Test]
        public void EnablingSplash_MovesListedSplashSceneFirst()
        {
            Assert.IsTrue(AssetDatabase.CopyAsset(SplashSceneSetup.k_TemplatePath, SplashScenePath));

            var scenes = SplashSceneSetup.UpdateScenes(true, BuildScenes(k_OtherToken, k_SplashToken), k_TestFolder);

            AssertScenePaths(new[] { SplashScenePath, k_OtherScenePath }, scenes);
        }

        [Test]
        public void DisablingSplash_RemovesSplashScene()
        {
            var scenes = SplashSceneSetup.UpdateScenes(false, BuildScenes(k_SplashToken, k_OtherToken), k_TestFolder);

            AssertScenePaths(new[] { k_OtherScenePath }, scenes);
        }

        [TestCase(true, true, "Splash,Other", "PlayerSplash", TestName = "SplashMessages(Player Settings splash on)")]
        [TestCase(true, false, "Other", "Missing", TestName = "SplashMessages(splash scene missing)")]
        [TestCase(true, false, "Other,Splash", "NotFirst", TestName = "SplashMessages(splash scene not first)")]
        [TestCase(true, false, "Splash", "NoSceneAfter", TestName = "SplashMessages(no scene after the splash)")]
        [TestCase(true, false, "Splash,Other", "", TestName = "SplashMessages(all correct)")]
        [TestCase(false, true, "Other", "", TestName = "SplashMessages(splash disabled)")]
        public void SplashMessages(
            bool splashEnabled, bool playerSplashShown, string sceneTokens, string expectedTokens)
        {
            var messages = SplashSceneSetup.GetMessages(
                splashEnabled, playerSplashShown, BuildScenes(sceneTokens.Split(',')), k_TestFolder);

            var expected = new List<(string, HelpBoxMessageType)>();
            foreach (var token in expectedTokens.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                expected.Add(GetMessage(token));
            }

            CollectionAssert.AreEqual(expected, messages);
        }

        static EditorBuildSettingsScene[] BuildScenes(params string[] tokens)
        {
            var scenes = new EditorBuildSettingsScene[tokens.Length];
            for (var i = 0; i < tokens.Length; i++)
            {
                var path = tokens[i] == k_SplashToken ? SplashScenePath : k_OtherScenePath;
                scenes[i] = new EditorBuildSettingsScene(path, true);
            }

            return scenes;
        }

        static (string, HelpBoxMessageType) GetMessage(string token)
        {
            return token switch
            {
                "PlayerSplash" => (SplashSceneSetup.k_PlayerSplashWarning, HelpBoxMessageType.Warning),
                "Missing" => (SplashSceneSetup.k_SceneMissingError, HelpBoxMessageType.Error),
                "NotFirst" => (SplashSceneSetup.k_SceneNotFirstError, HelpBoxMessageType.Error),
                "NoSceneAfter" => (SplashSceneSetup.k_NoSceneAfterWarning, HelpBoxMessageType.Warning),
                _ => throw new ArgumentOutOfRangeException(nameof(token), token, null),
            };
        }

        static void AssertScenePaths(string[] expectedPaths, EditorBuildSettingsScene[] scenes)
        {
            var paths = new string[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                paths[i] = scenes[i].path;
                Assert.IsTrue(scenes[i].enabled, $"{paths[i]} is disabled.");
            }

            CollectionAssert.AreEqual(expectedPaths, paths);
        }
    }
}
