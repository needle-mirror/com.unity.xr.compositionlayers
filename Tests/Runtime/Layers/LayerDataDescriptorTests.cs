using System;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Logic tests for layer-type discovery via <see cref="CompositionLayerUtils"/>: descriptor
    /// lookup, layer data creation, and the PreferOverlay attribute values.
    /// </summary>
    class LayerDataDescriptorTests
    {
        // The user-facing concrete layer types (DefaultLayerData is intentionally excluded).
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

        [Test]
        public void GetLayerDescriptor_ByType_ReturnsMatchingDescriptor(
            [ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var descriptor = CompositionLayerUtils.GetLayerDescriptor(layerType);

            Assert.AreNotEqual(LayerDataDescriptor.Empty, descriptor);
            Assert.AreEqual(layerType.FullName, descriptor.TypeFullName);
            Assert.AreEqual(layerType, descriptor.DataType);
        }

        [Test]
        public void CreateLayerData_ByFullName_ReturnsInstanceOfType(
            [ValueSource(nameof(s_UserLayerTypes))] Type layerType)
        {
            var data = CompositionLayerUtils.CreateLayerData(layerType.FullName);

            Assert.IsNotNull(data);
            Assert.IsInstanceOf(layerType, data);
        }

        [Test]
        public void GetLayerDescriptor_UnknownType_ReturnsEmpty()
        {
            var descriptor = CompositionLayerUtils.GetLayerDescriptor("Not.A.Real.LayerData");

            Assert.AreEqual(LayerDataDescriptor.Empty, descriptor);
        }

        [Test]
        public void PreferOverlay_MatchesAttributeValues()
        {
            // Quad/Cylinder do not set PreferOverlay, so they inherit the attribute default (true).
            Assert.IsTrue(CompositionLayerUtils.GetLayerDescriptor(typeof(QuadLayerData)).PreferOverlay);
            Assert.IsTrue(CompositionLayerUtils.GetLayerDescriptor(typeof(CylinderLayerData)).PreferOverlay);

            // Equirect and Cube explicitly opt out of overlay preference.
            Assert.IsFalse(CompositionLayerUtils.GetLayerDescriptor(typeof(EquirectMeshLayerData)).PreferOverlay);
            Assert.IsFalse(CompositionLayerUtils.GetLayerDescriptor(typeof(CubeProjectionLayerData)).PreferOverlay);
        }

        [Test]
        public void AllRegisteredDescriptors_RoundTrip()
        {
            foreach (var descriptor in CompositionLayerUtils.GetAllLayerDescriptors())
            {
                Assert.AreEqual(descriptor, CompositionLayerUtils.GetLayerDescriptor(descriptor.TypeFullName),
                    $"Descriptor lookup by full name did not round-trip for {descriptor.TypeFullName}.");

                var data = CompositionLayerUtils.CreateLayerData(descriptor.TypeFullName);
                Assert.IsNotNull(data, $"CreateLayerData returned null for {descriptor.TypeFullName}.");
                Assert.AreEqual(descriptor.DataType, data.GetType());
            }
        }
    }
}
