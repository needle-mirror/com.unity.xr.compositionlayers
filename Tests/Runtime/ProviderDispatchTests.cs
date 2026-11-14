using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    // Pins how the manager currently dispatches to providers. UpdateProviders broadcasts the same lists to
    // the native and emulation providers, so every layer reaches both. Per-layer fallback requires each
    // provider to see only its own layers, and these tests are what makes that change visible rather than
    // silent when it lands.
    class ProviderDispatchTests : CompositionLayerManagerTestBase
    {
        [Test]
        public async Task BothProvidersReceiveTheSameLayer()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();

            Assert.IsTrue(m_NativeRecorder.WasCreated(layer), "Native provider never saw the layer created.");
            Assert.IsTrue(m_EmulationRecorder.WasCreated(layer), "Emulation provider never saw the layer created.");

            Assert.IsTrue(m_NativeRecorder.WasActive(layer), "Native provider never saw the layer active.");
            Assert.IsTrue(m_EmulationRecorder.WasActive(layer), "Emulation provider never saw the layer active.");
        }

        [Test]
        public async Task BothProvidersReceiveTheSameRemoval()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();

            Assert.IsTrue(CompositionLayerManager.TryGetLayerId(layer, out var layerId));

            UnityObject.DestroyImmediate(layer.gameObject);
            await NextManagerUpdate();

            Assert.IsTrue(m_NativeRecorder.WasRemoved(layerId), "Native provider never saw the removal.");
            Assert.IsTrue(m_EmulationRecorder.WasRemoved(layerId), "Emulation provider never saw the removal.");
        }

        [Test]
        public async Task EveryUpdateCycleReportsToBothProviders()
        {
            CreateActiveLayer<QuadLayerData>();

            await NextManagerUpdate();
            await NextManagerUpdate();

            Assert.GreaterOrEqual(m_NativeRecorder.Reports.Count, 2);
            Assert.AreEqual(m_NativeRecorder.Reports.Count, m_EmulationRecorder.Reports.Count,
                "The two providers were not updated the same number of times.");
        }
    }
}
