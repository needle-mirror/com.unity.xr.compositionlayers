using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Tests <see cref="CompositionLayer.ChangeLayerDataType(System.Type)"/> overloads,
    /// extension population, and that changing the type of an already-active layer
    /// reports a modification (not a creation) to the layer provider.
    /// </summary>
    class LayerTypeChangeTests : CompositionLayerManagerTestBase
    {
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static readonly Type[] s_UserLayerTypes =
        {
            typeof(QuadLayerData),
            typeof(CylinderLayerData),
            typeof(EquirectMeshLayerData),
            typeof(CubeProjectionLayerData),
            typeof(ProjectionLayerData),
            typeof(ProjectionLayerRigData),
        };

        // CompositionLayerData attribute that use a TexturesExtension.
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static readonly Type[] s_TextureSuggestingTypes =
        {
            typeof(QuadLayerData),
            typeof(CylinderLayerData),
            typeof(EquirectMeshLayerData),
            typeof(CubeProjectionLayerData),
            typeof(ProjectionLayerData),
        };

        [Test]
        public void ChangeLayerDataType_TypeOverload([ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var layer = CreateTrackedLayer();

            layer.ChangeLayerDataType(layerType);

            Assert.IsNotNull(layer.LayerData);
            Assert.AreEqual(layerType, layer.LayerData.GetType());
        }

        [Test]
        public void ChangeLayerDataType_StringOverload([ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var layer = CreateTrackedLayer();

            layer.ChangeLayerDataType(layerType.FullName);

            Assert.IsNotNull(layer.LayerData);
            Assert.AreEqual(layerType, layer.LayerData.GetType());
        }

        [Test]
        public void ChangeLayerDataType_DescriptorOverload([ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var descriptor = CompositionLayerUtils.GetLayerDescriptor(layerType);
            var layer = CreateTrackedLayer();

            layer.ChangeLayerDataType(descriptor);

            Assert.IsNotNull(layer.LayerData);
            Assert.AreEqual(layerType, layer.LayerData.GetType());
        }

        [Test]
        public void ChangeLayerDataType_InstanceOverload([ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var data = CompositionLayerUtils.CreateLayerData(layerType.FullName);
            var layer = CreateTrackedLayer();

            layer.ChangeLayerDataType(data);

            Assert.AreSame(data, layer.LayerData);
        }

        [Test]
        public void ChangeLayerDataType_GenericOverload()
        {
            var layer = CreateTrackedLayer();

            layer.ChangeLayerDataType<QuadLayerData>();
            Assert.IsInstanceOf<QuadLayerData>(layer.LayerData);

            layer.ChangeLayerDataType<CylinderLayerData>();
            Assert.IsInstanceOf<CylinderLayerData>(layer.LayerData);
        }

        [Test]
        public void AddSuggestedExtensions_AddsTexturesExtension(
            [ValueSource(nameof(s_TextureSuggestingTypes))] Type layerType)
        {
            var layer = CreateTrackedLayer(layerType);

            layer.AddSuggestedExtensions();

            Assert.IsNotNull(layer.gameObject.GetComponent<TexturesExtension>());
        }

        [Test]
        public void AddSuggestedExtensions_NoneForProjectionRig()
        {
            var layer = CreateTrackedLayer(typeof(ProjectionLayerRigData));

            layer.AddSuggestedExtensions();

            Assert.IsNull(layer.gameObject.GetComponent<TexturesExtension>());
        }

        [Test]
        public async Task ChangingTypeOnActiveLayer_ReportsModifiedNotCreated()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await new WaitForUpdateCall(this);
            await new WaitForUpdateCall(this); // settle initial creation/modification

            layer.ChangeLayerDataType<CylinderLayerData>();
            await new WaitForUpdateCall(this);

            Assert.IsInstanceOf<CylinderLayerData>(layer.LayerData);
            Assert.IsTrue(WasReportedModified(layer));
            Assert.IsFalse(WasReportedCreated(layer));
        }
    }
}
