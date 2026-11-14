using System;
using System.Collections;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RuntimeSettingsPageTests : RuntimeSettingsTestBase
    {
        const int k_BindingFrames = 3;

        // Bindings poll for changes made outside the page, so give them several updates.
        const int k_ExternalChangeFrames = 30;
        const string k_EnableSplashScreenPath = "m_EnableSplashScreen";
        const string k_EmulationInStandalonePath = "m_EmulationInStandalone";
        const string k_CreateCase = "create";
        const string k_MigrateCase = "migrate";
        const string k_SetUpCase = "set up";
        const string k_SplashFolder = k_TestFolder + "/Splash";
        const string k_OtherScenePath = k_TestFolder + "/Other.unity";

        EditorWindow m_Window;
        CompositionLayersSettingsProvider m_Provider;
        EditorBuildSettingsScene[] m_BuildScenes;
        Object[] m_PreloadedAssets;

        [SetUp]
        public void CreateProjectLists()
        {
            m_BuildScenes = new[] { new EditorBuildSettingsScene(k_OtherScenePath, true) };
            m_PreloadedAssets = Array.Empty<Object>();
        }

        [TearDown]
        public void ClosePage()
        {
            m_Provider?.OnDeactivate();
            m_Provider = null;
            if (m_Window != null)
                m_Window.Close();
        }

        [UnityTest]
        public IEnumerator Page_SetUpButton_SetsUp([Values(k_CreateCase, k_MigrateCase, k_SetUpCase)] string setUpCase)
        {
            if (setUpCase == k_MigrateCase)
                CreateSettingsAsset(Locations.LegacyAssetPath);
            else if (setUpCase == k_SetUpCase)
                CreateSettingsAsset(Locations.DefaultAssetPath);

            var filesBefore = GetFilesInTestFolder();
            var root = OpenPage();
            yield return WaitForBinding();

            CollectionAssert.AreEquivalent(filesBefore, GetFilesInTestFolder(), "Opening the page changed files.");
            CollectionAssert.IsEmpty(m_PreloadedAssets, "Opening the page changed Preloaded Assets.");
            ClickButton(root, CompositionLayersSettingsProvider.k_SetUpButtonName);
            yield return WaitForBinding();

            Assert.IsTrue(RuntimeSettingsSetup.IsSetUp(Locations, m_PreloadedAssets), "The project isn't set up.");
            Assert.IsNull(GetSetupError(root));
            Assert.IsNull(root.Q<Button>(CompositionLayersSettingsProvider.k_SetUpButtonName));
        }

        [UnityTest]
        public IEnumerator Page_NewAndOldAssets_EditsNewAsset()
        {
            var oldSettings = CreateSettingsAsset(Locations.LegacyAssetPath);
            var newSettings = CreateSettingsAsset(Locations.DefaultAssetPath);
            var root = OpenPage();
            yield return WaitForBinding();

            var toggle = FindPropertyField(root, k_EmulationInStandalonePath)?.Q<Toggle>();
            Assert.IsNotNull(toggle, "The page has no Emulation In Standalone toggle.");
            toggle.value = true;
            yield return WaitForBinding();

            Assert.IsTrue(new SerializedObject(newSettings).FindProperty(k_EmulationInStandalonePath).boolValue);
            Assert.IsFalse(new SerializedObject(oldSettings).FindProperty(k_EmulationInStandalonePath).boolValue);
        }

        [UnityTest]
        public IEnumerator Page_EnablingSplash_EditsAssetAndBuildScenes()
        {
            var settings = CreateSettingsAsset(Locations.DefaultAssetPath);
            var root = OpenPage();
            yield return WaitForBinding();

            var toggle = root.Q<Toggle>(CompositionLayersSettingsProvider.k_EnableSplashToggleName);
            Assert.IsNotNull(toggle, "The page has no Enable Splash Screen toggle.");
            toggle.value = true;
            yield return WaitForBinding();

            Assert.IsTrue(new SerializedObject(settings).FindProperty(k_EnableSplashScreenPath).boolValue);
            var splashScenePath = SplashSceneSetup.GetScenePath(k_SplashFolder);
            CollectionAssert.AreEqual(new[] { splashScenePath, k_OtherScenePath }, GetScenePaths(m_BuildScenes));
            Assert.IsTrue(AssetDatabase.AssetPathExists(splashScenePath));
        }

        // Undo or a version control revert changes the asset without the user toggling the page.
        [UnityTest]
        public IEnumerator Page_SplashChangedOutsideThePage_LeavesBuildScenes()
        {
            var settings = CreateSettingsAsset(Locations.DefaultAssetPath);
            OpenPage();
            yield return WaitForBinding();

            var serialized = new SerializedObject(settings);
            serialized.FindProperty(k_EnableSplashScreenPath).boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            for (var i = 0; i < k_ExternalChangeFrames; i++)
            {
                yield return null;
            }

            CollectionAssert.AreEqual(new[] { k_OtherScenePath }, GetScenePaths(m_BuildScenes));
            Assert.IsFalse(AssetDatabase.IsValidFolder(k_SplashFolder));
        }

        VisualElement OpenPage()
        {
            m_Window = EditorWindow.CreateInstance<EditorWindow>();
            m_Window.Show();
            var splashScenes = new SplashSceneTarget(
                k_SplashFolder, () => m_BuildScenes, scenes => m_BuildScenes = scenes);
            m_Provider = new CompositionLayersSettingsProvider(
                Locations, () => m_PreloadedAssets, assets => m_PreloadedAssets = assets, splashScenes);
            var root = new VisualElement();
            m_Window.rootVisualElement.Add(root);
            m_Provider.OnActivate("", root);
            return root;
        }

        static void ClickButton(VisualElement root, string buttonName)
        {
            var button = root.Q<Button>(buttonName);
            Assert.IsNotNull(button, $"The page has no {buttonName} button.");
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        static HelpBox GetSetupError(VisualElement root)
        {
            return root.Q<HelpBox>(CompositionLayersSettingsProvider.k_SetupErrorName);
        }

        static IEnumerator WaitForBinding()
        {
            for (var i = 0; i < k_BindingFrames; i++)
            {
                yield return null;
            }
        }

        static string[] GetScenePaths(EditorBuildSettingsScene[] scenes)
        {
            var paths = new string[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                paths[i] = scenes[i].path;
            }

            return paths;
        }

        static PropertyField FindPropertyField(VisualElement root, string bindingPath)
        {
            foreach (var field in root.Query<PropertyField>().ToList())
            {
                if (field.bindingPath == bindingPath)
                    return field;
            }

            return null;
        }
    }
}
