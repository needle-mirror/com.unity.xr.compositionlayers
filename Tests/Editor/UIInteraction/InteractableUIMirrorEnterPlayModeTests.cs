#if UNITY_XR_INTERACTION_TOOLKIT
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Tests.UIInteraction;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.XR.CompositionLayers.Tests.Editor.UIInteraction
{
    class InteractableUIMirrorEnterPlayModeTests : UIPanelTestBase
    {
        const EnterPlayModeOptions k_NoDomainOrSceneReload =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        bool m_WereEnterPlayModeOptionsEnabled;
        EnterPlayModeOptions m_PreviousEnterPlayModeOptions;

        protected override void Setup()
        {
            m_WereEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_PreviousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            base.Setup();
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
                    await NextEditorUpdateAsync();
            }

            EditorSettings.enterPlayModeOptionsEnabled = m_WereEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = m_PreviousEnterPlayModeOptions;
        }

        // Entering Play mode without a domain reload leaves static state behind, such as subsystem descriptors
        // registered on SubsystemRegistration, which later Editor tests do not expect. Only an IEnumerator
        // teardown survives the reload that clears it.
        [UnityTearDown]
        public IEnumerator ReloadDomain()
        {
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();
        }

        [Test]
        public async Task ForcedHover_WithoutDomainAndSceneReload_EnablesCanvasRaycasts()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = k_NoDomainOrSceneReload;

            var panel = CreatePanel();
            EditorApplication.QueuePlayerLoopUpdate();
            await NextEditorUpdateAsync();

            // The Editor skips the edit mode player loop while another application is active, so Start may not have
            // run. This does nothing when it has.
            panel.InitializeLayerAndCamera();

            // The edit mode setup is what Play mode inherits when the scene is not reloaded.
            Assert.IsNotNull(GetInteractable(panel));

            EditorApplication.isPlaying = true;
            while (!EditorApplication.isPlaying)
                await NextEditorUpdateAsync();

            await NextEditorUpdateAsync();

            var interactor = CreateInteractor(panel);
            Assert.IsFalse(CanvasBlocksRaycasts(panel));

            ForceHover(interactor, panel);

            Assert.IsTrue(GetInteractable(panel).isHovered);
            Assert.IsTrue(CanvasBlocksRaycasts(panel));
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
#endif
