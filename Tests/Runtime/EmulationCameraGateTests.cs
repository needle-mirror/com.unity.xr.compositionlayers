using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    // In Play mode, a layer's RenderPath alone decides whether the main camera draws emulation. Uses the real
    // EmulatedLayerProvider, since the camera choice is made inside it.
    class EmulationCameraGateTests : CompositionLayerManagerTestBase
    {
        Camera m_MainCamera;

        protected override void Setup()
        {
            base.Setup();

            m_MainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            m_MainCamera.tag = "MainCamera";
            EmulatedLayerProvider.ConnectEmulatedLayerProvider();
        }

        protected override void Teardown()
        {
            EmulatedLayerProvider.DisconnectEmulatedLayerProvider();
            if (m_MainCamera != null)
                UnityObject.DestroyImmediate(m_MainCamera.gameObject);

            base.Teardown();
        }

        [Test]
        public async Task EmulatedLayer_SelectsMainCamera()
        {
            ForgetNativeProvider();

            var layer = CreateActiveLayer<QuadLayerData>();
            await NextEmulationProviderUpdate();

            // The hidden Default Scene Layer is emulated too, so make it native to isolate the quad.
            var defaultLayer = CompositionLayerManager.Instance.DefaultSceneCompositionLayer;
            defaultLayer.RenderPath = CompositionLayerRenderPath.Native;
            await NextEmulationProviderUpdate();

            Assert.AreEqual(CompositionLayerRenderPath.Emulated, layer.RenderPath);
            AssertMainCameraSelected(true);
        }

        [Test]
        public async Task NativeLayersOnly_SelectNoCamera()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextEmulationProviderUpdate();

            Assert.AreEqual(CompositionLayerRenderPath.Native, layer.RenderPath);
            AssertMainCameraSelected(false);
        }

        [Test]
        public async Task DefaultLayerOnly_NoProvider_SelectsMainCamera()
        {
            ForgetNativeProvider();

            var defaultLayer = CreateActiveLayer<DefaultLayerData>();
            await NextEmulationProviderUpdate();

            Assert.AreSame(defaultLayer, CompositionLayerManager.Instance.DefaultSceneCompositionLayer);
            Assert.AreEqual(CompositionLayerRenderPath.Emulated, defaultLayer.RenderPath);
            AssertMainCameraSelected(true);
        }

        // The real provider replaces the emulation recorder, so cycles are counted in frames rather than reports.
        static async Task NextEmulationProviderUpdate()
        {
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
        }

        void AssertMainCameraSelected(bool expected)
        {
            Assert.AreSame(m_MainCamera, CompositionLayerManager.MainCameraCache, "Test camera is not main.");
            Assert.AreEqual(expected, EmulatedLayerProvider.IsSupported(m_MainCamera));
        }
    }
}
