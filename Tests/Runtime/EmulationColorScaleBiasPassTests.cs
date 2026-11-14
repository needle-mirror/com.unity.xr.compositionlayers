using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Tests
{
    // In Play mode, Game cameras get the scene ColorScaleBias from the emulation pass only while the Default Scene
    // Layer is emulated, so the native compositor and the pass never both apply it. The Scene View previews it
    // whatever the render path, like the emulated layers it shows.
    class EmulationColorScaleBiasPassTests : CompositionLayerManagerTestBase
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task ColorScaleBiasPass_FollowsDefaultLayerRenderPath(bool providerSet)
        {
            if (!providerSet)
                ForgetNativeProvider();

            var defaultLayer = CreateActiveLayer<DefaultLayerData>();
            var colorScaleBias = defaultLayer.gameObject.AddComponent<ColorScaleBiasExtension>();
            await NextEmulationUpdate();

            var expectedRenderPath =
                providerSet ? CompositionLayerRenderPath.Native : CompositionLayerRenderPath.Emulated;
            var expectedColorScaleBias = providerSet ? null : colorScaleBias;
            var appliedColorScaleBias =
                EmulatedCompositionLayerUtils.GetEmulatedColorScaleBias(defaultLayer, CameraType.Game);
            Assert.AreSame(defaultLayer, CompositionLayerManager.Instance.DefaultSceneCompositionLayer);
            Assert.AreEqual(expectedRenderPath, defaultLayer.RenderPath);
            Assert.AreSame(expectedColorScaleBias, appliedColorScaleBias);
        }

        [Test]
        public async Task ColorScaleBiasPass_SceneViewPreviewsWhileDefaultLayerIsNative()
        {
            var defaultLayer = CreateActiveLayer<DefaultLayerData>();
            var colorScaleBias = defaultLayer.gameObject.AddComponent<ColorScaleBiasExtension>();
            await NextManagerUpdate();

            var appliedColorScaleBias =
                EmulatedCompositionLayerUtils.GetEmulatedColorScaleBias(defaultLayer, CameraType.SceneView);
            Assert.AreEqual(CompositionLayerRenderPath.Native, defaultLayer.RenderPath);
            Assert.AreSame(colorScaleBias, appliedColorScaleBias);
        }
    }
}
