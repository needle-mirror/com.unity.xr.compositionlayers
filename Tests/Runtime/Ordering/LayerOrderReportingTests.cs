using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Tests.Ordering
{
    /// <summary>
    /// Tests how layer order/activation changes are reported to the layer provider.
    /// Changing the order of an active layer reports it as modified,
    /// active layers are reported and sorted by ascending order, and
    /// disabling a layer removes it from the active set while reporting it as modified.
    /// </summary>
    class LayerOrderReportingTests : CompositionLayerManagerTestBase
    {
        [Test]
        public async Task ChangingOrder_ReportsModified()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);
            await new WaitForUpdateCall(this); // settle

            // 42 is well clear of the default low-order layers, so the change should succeed.
            layer.Order = 42;
            await new WaitForUpdateCall(this);

            Assert.AreEqual(42, layer.Order);
            Assert.IsTrue(WasReportedModified(layer));
        }

        [Test]
        public async Task ActiveLayers_ReportedInAscendingOrder()
        {
            CreateActiveLayer(typeof(QuadLayerData), order: 5);
            CreateActiveLayer(typeof(QuadLayerData), order: 3);
            await new WaitForUpdateCall(this);

            // Regardless of the exact assigned orders, the active list is sorted ascending by Order.
            for (var i = 1; i < lastActiveLayers.Count; i++)
            {
                Assert.LessOrEqual(
                    lastActiveLayers[i - 1].Layer.Order,
                    lastActiveLayers[i].Layer.Order,
                    "Active layers should be reported sorted ascending by order.");
            }
        }

        [Test]
        public async Task DisablingLayer_RemovesFromActive_AndReportsModified()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);
            Assert.IsTrue(WasReportedActive(layer));

            layer.gameObject.SetActive(false);
            await new WaitForUpdateCall(this);

            Assert.IsFalse(WasReportedActive(layer));
            Assert.IsTrue(WasReportedModified(layer));
        }
    }
}
