using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Tests
{
    // Dispatch still broadcasts to both providers, so these tests assert RenderPath only.
    //
    // Once a provider has been set, evaluating any layer gives Native, so a test that only checks the result
    // cannot tell a skipped evaluation from one that ran. The "does not re-evaluate" tests force a probe layer
    // to Emulated first: it stays Emulated only if nothing evaluated it.
    class RenderPathEvaluationTests : CompositionLayerManagerTestBase
    {
        static CompositionLayer DefaultSceneLayer => CompositionLayerManager.Instance.DefaultSceneCompositionLayer;

        [Test]
        public async Task NoProviderEverSet_LayersAreEmulated()
        {
            ForgetNativeProvider();

            var layer = CreateActiveLayer<QuadLayerData>();
            await NextEmulationUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Emulated, layer, DefaultSceneLayer);
        }

        [Test]
        public async Task NoProviderEverSet_ProjectionRigLayerIsEmulated()
        {
            ForgetNativeProvider();

            var rig = CreateActiveLayer<ProjectionLayerRigData>();
            var quad = CreateActiveLayer<QuadLayerData>();
            await NextEmulationUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Emulated, rig, quad);
        }

        [Test]
        public async Task ProviderSetAfterProjectionRigEnabled_RigBecomesNative()
        {
            ForgetNativeProvider();

            var rig = CreateActiveLayer<ProjectionLayerRigData>();
            await NextEmulationUpdate();
            AssertRenderPath(CompositionLayerRenderPath.Emulated, rig);

            ReattachNativeProvider();

            AssertRenderPath(CompositionLayerRenderPath.Native, rig);
        }

        [Test]
        public async Task ProviderSetBeforeEnable_LayerIsNative()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Native, layer, DefaultSceneLayer);
        }

        [Test]
        public async Task ProviderSetAfterLayersEnabled_AllLayersBecomeNative()
        {
            ForgetNativeProvider();

            var quad = CreateActiveLayer<QuadLayerData>();
            var cylinder = CreateActiveLayer<CylinderLayerData>();
            await NextEmulationUpdate();
            AssertRenderPath(CompositionLayerRenderPath.Emulated, quad, cylinder, DefaultSceneLayer);

            ReattachNativeProvider();

            AssertRenderPath(CompositionLayerRenderPath.Native, quad, cylinder, DefaultSceneLayer);
        }

        [Test]
        public async Task DisabledLayerWhenProviderSet_IsNativeOnReenable()
        {
            ForgetNativeProvider();

            var layer = CreateActiveLayer<QuadLayerData>();
            await NextEmulationUpdate();
            AssertRenderPath(CompositionLayerRenderPath.Emulated, layer);

            layer.gameObject.SetActive(false);
            await NextEmulationUpdate();

            // Setting the provider already evaluates known layers, so the probe isolates evaluation on enable.
            ReattachNativeProvider();
            layer.RenderPath = CompositionLayerRenderPath.Emulated;
            layer.gameObject.SetActive(true);
            await NextManagerUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Native, layer);
        }

        [Test]
        public async Task ProviderCleared_DoesNotReevaluate()
        {
            var probe = CreateActiveLayer<QuadLayerData>();
            var other = CreateActiveLayer<CylinderLayerData>();
            await NextManagerUpdate();

            probe.RenderPath = CompositionLayerRenderPath.Emulated;
            DetachNativeProvider();
            await NextEmulationUpdate();
            await NextEmulationUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Emulated, probe);
            AssertRenderPath(CompositionLayerRenderPath.Native, other, DefaultSceneLayer);
        }

        [Test]
        public async Task LayerEnabledWhileDetached_IsNative()
        {
            DetachNativeProvider();

            var layer = CreateActiveLayer<QuadLayerData>();
            await NextEmulationUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Native, layer, DefaultSceneLayer);
        }

        [Test]
        public async Task ClearThenSetAgain_NoRenderPathTransitions()
        {
            var quad = CreateActiveLayer<QuadLayerData>();
            var cylinder = CreateActiveLayer<CylinderLayerData>();
            await NextManagerUpdate();

            DetachNativeProvider();
            await NextEmulationUpdate();
            AssertRenderPath(CompositionLayerRenderPath.Native, quad, cylinder, DefaultSceneLayer);

            ReattachNativeProvider();
            await NextManagerUpdate();
            AssertRenderPath(CompositionLayerRenderPath.Native, quad, cylinder, DefaultSceneLayer);
        }

        [TestCase(typeof(QuadLayerData), true)]
        [TestCase(typeof(ProjectionLayerData), false)]
        public async Task StateChangedWithoutTypeChange_DoesNotReevaluate(Type layerDataType, bool moveTransform)
        {
            var probe = CreateActiveLayer(layerDataType);

            // A new transform reports hasChanged on its first frame, so let that settle before forcing the probe.
            await NextManagerUpdate();
            await NextManagerUpdate();

            probe.RenderPath = CompositionLayerRenderPath.Emulated;
            m_NativeRecorder.Clear();

            if (moveTransform)
                probe.transform.position += Vector3.right;

            await NextManagerUpdate();
            await NextManagerUpdate();

            Assert.IsTrue(m_NativeRecorder.WasModified(probe), "The layer never reported a state change.");
            AssertRenderPath(CompositionLayerRenderPath.Emulated, probe);
        }

        [Test]
        public async Task LayerDataTypeChange_Reevaluates()
        {
            var probe = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();

            probe.RenderPath = CompositionLayerRenderPath.Emulated;
            probe.ChangeLayerDataType<CylinderLayerData>();
            await NextManagerUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Native, probe);
        }

        [Test]
        public async Task SecondDefaultLayerData_FollowsProviderPresence()
        {
            ForgetNativeProvider();

            CreateActiveLayer<QuadLayerData>();
            await NextEmulationUpdate();

            // DefaultLayerData.Validate destroys a duplicate in OnEnable, so register it with the manager directly,
            // the way a duplicate can register for a frame under XR-10765.
            var duplicate = CreateTrackedLayer(typeof(DefaultLayerData));
            CompositionLayerManager.Instance.CompositionLayerCreated(duplicate);
            CompositionLayerManager.Instance.CompositionLayerEnabled(duplicate);
            await NextEmulationUpdate();

            Assert.AreNotSame(DefaultSceneLayer, duplicate);
            AssertRenderPath(CompositionLayerRenderPath.Emulated, duplicate);

            ReattachNativeProvider();
            await NextManagerUpdate();

            AssertRenderPath(CompositionLayerRenderPath.Native, duplicate);
        }

        static void AssertRenderPath(CompositionLayerRenderPath expected, params CompositionLayer[] layers)
        {
            foreach (var layer in layers)
            {
                Assert.IsNotNull(layer, "Expected layer does not exist.");
                Assert.AreEqual(expected, layer.RenderPath, $"Layer '{layer.name}' has the wrong render path.");
            }
        }
    }
}
