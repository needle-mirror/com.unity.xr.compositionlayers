using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.Scripting.LifecycleManagement;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Tests <see cref="CompositionLayer.ChangeLayerDataType(System.Type)"/> overloads,
    /// extension population, and that changing the type of an already-active layer
    /// reports a modification (not a creation) to the layer provider.
    /// </summary>
    class LayerTypeChangeTests : CompositionLayerManagerTestBase
    {
        [NoAutoStaticsCleanup]
        static readonly Regex k_UnregisteredTypeWarning = new Regex("isn't a registered layer data type");

        [NoAutoStaticsCleanup]
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
        [NoAutoStaticsCleanup]
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
        public void ChangeLayerDataType_UnknownType_KeepsLayerData()
        {
            var layer = CreateTrackedLayer(typeof(QuadLayerData));
            var layerData = layer.LayerData;

            LogAssert.Expect(LogType.Warning, k_UnregisteredTypeWarning);
            layer.ChangeLayerDataType("Not.A.Real.LayerData");
            Assert.AreSame(layerData, layer.LayerData);

            LogAssert.Expect(LogType.Warning, k_UnregisteredTypeWarning);
            layer.ChangeLayerDataType(typeof(string));
            Assert.AreSame(layerData, layer.LayerData);

            LogAssert.Expect(LogType.Warning, k_UnregisteredTypeWarning);
            layer.ChangeLayerDataType(LayerDataDescriptor.Empty);
            Assert.AreSame(layerData, layer.LayerData);
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
            await NextManagerUpdate();
            await NextManagerUpdate(); // settle initial creation/modification

            layer.ChangeLayerDataType<CylinderLayerData>();
            await NextManagerUpdate();

            Assert.IsInstanceOf<CylinderLayerData>(layer.LayerData);
            Assert.IsTrue(WasReportedModified(layer));
            Assert.IsFalse(WasReportedCreated(layer));
        }
    }
}
