using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Logic tests for <see cref="LayerData.CopyFrom"/> on different layer data types.
    /// Test the current copy semantics, including switching Layer types.
    /// Test the base no-op behavior for incompatible types.
    /// </summary>
    class LayerDataCopyFromTests
    {
        const float k_Epsilon = 1e-4f;

        [Test]
        public void Quad_CopyFrom_CopiesSizeAndApplyTransformScale()
        {
            var source = new QuadLayerData { Size = new Vector2(3f, 4f), ApplyTransformScale = false };
            var destination = new QuadLayerData();

            destination.CopyFrom(source);

            Assert.AreEqual(3f, destination.Size.x, k_Epsilon);
            Assert.AreEqual(4f, destination.Size.y, k_Epsilon);
            Assert.IsFalse(destination.ApplyTransformScale);
        }

        [Test]
        public void Cylinder_CopyFrom_CopiesRadiusAngleAndAspect()
        {
            var source = new CylinderLayerData { Radius = 5f, AspectRatio = 2f };
            source.CentralAngle = 1.2f;
            var destination = new CylinderLayerData();

            destination.CopyFrom(source);

            Assert.AreEqual(5f, destination.Radius, k_Epsilon);
            Assert.AreEqual(1.2f, destination.CentralAngle, k_Epsilon);
            Assert.AreEqual(2f, destination.AspectRatio, k_Epsilon);
        }

        [Test]
        public void Cylinder_CopyFrom_DoesNotCopyMaintainAspectOrTransformScale()
        {
            // Test the documented asymmetry in CylinderLayerData.CopyFrom: only Radius, CentralAngle,
            // and AspectRatio are copied. If this changes, this test should be updated.
            var source = new CylinderLayerData { MaintainAspectRatio = true, ApplyTransformScale = false };
            var destination = new CylinderLayerData();

            var originalMaintain = destination.MaintainAspectRatio; // default false
            var originalApply = destination.ApplyTransformScale;    // default true

            destination.CopyFrom(source);

            Assert.AreEqual(originalMaintain, destination.MaintainAspectRatio);
            Assert.AreEqual(originalApply, destination.ApplyTransformScale);
        }

        [Test]
        public void CopyFrom_MismatchedType_IsNoOp()
        {
            var quad = new QuadLayerData { Size = new Vector2(2f, 2f) };
            var cylinder = new CylinderLayerData { Radius = 9f };

            quad.CopyFrom(cylinder);

            Assert.AreEqual(2f, quad.Size.x, k_Epsilon);
            Assert.AreEqual(2f, quad.Size.y, k_Epsilon);
        }

        [Test]
        public void Equirect_CopyFrom_IsNoOp_FromBase()
        {
            // EquirectMeshLayerData does not override CopyFrom, so it inherits the base no-op.
            var source = new EquirectMeshLayerData { Radius = 5f };
            var destination = new EquirectMeshLayerData(); // default radius 100

            destination.CopyFrom(source);

            Assert.AreEqual(100f, destination.Radius, k_Epsilon);
        }
    }
}
