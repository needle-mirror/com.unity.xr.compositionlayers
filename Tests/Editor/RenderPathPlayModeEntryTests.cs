using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RenderPathPlayModeEntryTests : CompositionLayerManagerTestBase
    {
        const EnterPlayModeOptions k_NoDomainOrSceneReload =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        bool m_WereEnterPlayModeOptionsEnabled;
        EnterPlayModeOptions m_PreviousEnterPlayModeOptions;
        SceneSetup[] m_PreviousSceneSetup;

        // Entering Play mode without a scene reload enables whatever the open scene holds, including objects other
        // packages' tests leave behind, so the test runs in an empty scene.
        protected override void Setup()
        {
            m_WereEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_PreviousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            m_PreviousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            base.Setup();
        }

        protected override void Teardown()
        {
            base.Teardown();
            if (CanRestore(m_PreviousSceneSetup))
                EditorSceneManager.RestoreSceneManagerSetup(m_PreviousSceneSetup);
        }

        // Runs before the base class [TearDown], so its cleanup happens after exiting Play mode restores the
        // edit mode scene rather than being undone by it.
        [TearDown]
        public async Task ExitPlayModeAndRestoreSettings()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (EditorApplication.isPlaying)
                {
                    await NextEditorUpdateAsync();
                }
            }

            EditorSettings.enterPlayModeOptionsEnabled = m_WereEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = m_PreviousEnterPlayModeOptions;
        }

        // Entering Play mode without a domain reload leaves static state behind that later Editor tests do not
        // expect. Only an IEnumerator teardown survives the reload that clears it.
        [UnityTearDown]
        public IEnumerator ReloadDomain()
        {
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();
        }

        [Test]
        public async Task ProviderEverSet_ResetsOnPlayModeEntryWithoutDomainReload()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = k_NoDomainOrSceneReload;

            // The base class set a provider in Setup. Detaching keeps that history, as pausing a session does.
            DetachNativeProvider();

            EditorApplication.isPlaying = true;
            while (!EditorApplication.isPlaying)
            {
                await NextEditorUpdateAsync();
            }

            await NextEditorUpdateAsync();

            // A running XR display means XR started a session, which may set a provider of its own.
            Assume.That(CompositionLayerUtils.IsDisplaySubsystemActive(), Is.False, "Requires no running XR display.");

            var layer = CreateActiveLayer<QuadLayerData>();

            Assert.AreEqual(CompositionLayerRenderPath.Emulated, layer.RenderPath);
        }

        // Untitled scenes have no path to reload from.
        static bool CanRestore(SceneSetup[] sceneSetup)
        {
            if (sceneSetup.Length == 0)
                return false;

            foreach (var scene in sceneSetup)
            {
                if (string.IsNullOrEmpty(scene.path))
                    return false;
            }

            return true;
        }

        // Awaitable.NextFrameAsync follows the player loop, which does not tick every editor update in edit mode.
        static Awaitable NextEditorUpdateAsync()
        {
            var completion = new AwaitableCompletionSource();
            EditorApplication.CallbackFunction onUpdate = null;
            onUpdate = () =>
            {
                EditorApplication.update -= onUpdate;
                completion.SetResult();
            };

            EditorApplication.update += onUpdate;
            return completion.Awaitable;
        }
    }
}
