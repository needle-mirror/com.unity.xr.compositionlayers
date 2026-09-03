using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

namespace Unity.XR.CompositionLayers.Tests.Ordering
{
    /// <summary>
    /// Focused tests for layer order assignment and reordering functionalities.
    /// </summary>
    class LayerOrderingTests : CompositionLayerManagerTestBase
    {
        [Test]
        public async Task OverlayCollision_AssignsDistinctPositiveOrders()
        {
            // Both default to the same overlay order; the manager must resolve the collision.
            var first = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            var second = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            Assert.GreaterOrEqual(first.Order, 1);
            Assert.GreaterOrEqual(second.Order, 1);
            Assert.AreNotEqual(first.Order, second.Order);
        }

        [Test]
        public async Task TrySwapLayers_ExchangesOrders()
        {
            var a = CreateActiveLayer<QuadLayerData>();
            var b = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            var orderA = a.Order;
            var orderB = b.Order;
            Assert.AreNotEqual(orderA, orderB);

            Assert.IsTrue(CompositionLayerUtils.TrySwapLayers(a, b));
            await new WaitForUpdateCall(this);

            Assert.AreEqual(orderB, a.Order);
            Assert.AreEqual(orderA, b.Order);
        }

        [Test]
        public async Task TrySwapLayers_RejectsSameLayer()
        {
            var a = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            Assert.IsFalse(CompositionLayerUtils.TrySwapLayers(a, a));
        }

        [Test]
        public async Task TrySwapLayers_RejectsDefaultSceneLayer()
        {
            var a = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            var defaultLayer = CompositionLayerManager.Instance.DefaultSceneCompositionLayer;
            Assert.IsNotNull(defaultLayer, "A default scene layer should exist once a layer is active.");

            Assert.IsFalse(CompositionLayerUtils.TrySwapLayers(a, defaultLayer));
        }

        [Test]
        public async Task CanChangeOrderToZero_OnlyForDefaultSceneLayer()
        {
            var quad = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            var defaultLayer = CompositionLayerManager.Instance.DefaultSceneCompositionLayer;
            Assert.IsNotNull(defaultLayer);

            Assert.IsFalse(quad.CanChangeOrderTo(0), "A non-default layer must not be allowed at order 0.");
            Assert.IsTrue(defaultLayer.CanChangeOrderTo(0), "The default scene layer must be allowed at order 0.");
        }

        [Test]
        public async Task GetNextUnusedLayer_RespectsDirection()
        {
            // Ensure the manager is active with at least one occupied layer.
            CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);

            Assert.GreaterOrEqual(CompositionLayerManager.GetNextUnusedLayer(0, true), 1);
            Assert.LessOrEqual(CompositionLayerManager.GetNextUnusedLayer(0, false), -1);
            Assert.GreaterOrEqual(CompositionLayerManager.GetFirstUnusedLayer(), 1);

            // GetNextUnusedLayer(order) chooses overlay direction when order > -1.
            Assert.AreEqual(
                CompositionLayerManager.GetNextUnusedLayer(0, true),
                CompositionLayerManager.GetNextUnusedLayer(0));
        }
    }
}
