using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Services;
using UnityEditor;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class SceneViewEmulationTests
    {
        SceneView m_SceneView;
        bool m_StartedManager;

        [SetUp]
        public void SetUp()
        {
            m_SceneView = EditorWindow.CreateWindow<SceneView>();

            // The manager stops when a scene has no layers, and starting it lets the real emulation provider connect.
            m_StartedManager = !CompositionLayerManager.ManagerActive;
            if (m_StartedManager)
                CompositionLayerManager.StartCompositionLayerManager();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_StartedManager)
                CompositionLayerManager.StopCompositionLayerManager();

            if (m_SceneView != null)
                m_SceneView.Close();
        }

        // Calls the provider directly instead of waiting for a manager update, which an inactive Editor never runs.
        [Test]
        public void EditMode_SceneViewCamerasSelected()
        {
            var provider = CompositionLayerManager.Instance.EmulationLayerProvider as EmulatedLayerProvider;
            Assert.IsNotNull(provider, "Precondition: the real emulation provider is connected.");

            var noLayers = new List<CompositionLayerManager.LayerInfo>();
            provider.UpdateLayers(noLayers, new List<int>(), noLayers, noLayers);

            Assert.IsTrue(EmulatedLayerProvider.IsSupported(m_SceneView.camera));
        }
    }
}
