using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Logic tests for the geometry helpers on the layer data types.
    /// These operate on <see cref="LayerData"/> instances with no GameObject, scene, or manager.
    /// So, they are fast and device-independent.
    /// </summary>
    class LayerDataGeometryTests
    {
        const float k_Epsilon = 1e-4f;
        const float k_DegreesEpsilon = 1e-3f;

        // --- Quad ---

        [Test]
        public void Quad_GetScaledSize_AppliesScaleWhenEnabled()
        {
            var quad = new QuadLayerData { Size = new Vector2(2f, 3f), ApplyTransformScale = true };

            var scaled = quad.GetScaledSize(new Vector3(4f, 5f, 6f));

            Assert.AreEqual(8f, scaled.x, k_Epsilon);
            Assert.AreEqual(15f, scaled.y, k_Epsilon);
        }

        [Test]
        public void Quad_GetScaledSize_IgnoresScaleWhenDisabled()
        {
            var quad = new QuadLayerData { Size = new Vector2(2f, 3f), ApplyTransformScale = false };

            var scaled = quad.GetScaledSize(new Vector3(4f, 5f, 6f));

            Assert.AreEqual(2f, scaled.x, k_Epsilon);
            Assert.AreEqual(3f, scaled.y, k_Epsilon);
        }

        // --- Cylinder ---

        [Test]
        public void Cylinder_GetHeight_MatchesFormula()
        {

            var cylinder = new CylinderLayerData { Radius = 2f, AspectRatio = 2f };
            cylinder.CentralAngle = Mathf.PI;
            // GetHeight() == Radius * CentralAngle / AspectRatio
            Assert.AreEqual(2f * Mathf.PI / 2f, cylinder.GetHeight(), k_Epsilon);
        }

        [Test]
        public void Cylinder_CentralAngle_GetterClampsToTwoPi()
        {
            var cylinder = new CylinderLayerData();
            cylinder.CentralAngle = 10f; // greater than 2*PI (~6.283)

            Assert.AreEqual(Mathf.PI * 2f, cylinder.CentralAngle, k_Epsilon);
        }

        [Test]
        public void Cylinder_CentralAngleInDegrees_RoundTrips()
        {
            var cylinder = new CylinderLayerData();
            cylinder.CentralAngleInDegrees = 90f;

            Assert.AreEqual(90f, cylinder.CentralAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(Mathf.PI * 0.5f, cylinder.CentralAngle, k_Epsilon);
        }

        [Test]
        public void Cylinder_CalculateAspectRatioFromHeight_IsInverseOfGetHeight()
        {
            var cylinder = new CylinderLayerData { Radius = 2f, AspectRatio = 3f };
            cylinder.CentralAngle = 1f;

            var height = cylinder.GetHeight();

            Assert.AreEqual(3f, cylinder.CalculateAspectRatioFromHeight(height), k_Epsilon);
        }

        [Test]
        public void Cylinder_CalculateRadiusFromHeight_IsInverseOfGetHeight()
        {
            var cylinder = new CylinderLayerData { Radius = 2f, AspectRatio = 3f };
            cylinder.CentralAngle = 1f;

            var height = cylinder.GetHeight();

            Assert.AreEqual(2f, cylinder.CalculateRadiusFromHeight(height), k_Epsilon);
        }

        [Test]
        public void Cylinder_CalculateAspectRatioFromHeight_GuardsZeroRadius()
        {
            var cylinder = new CylinderLayerData { Radius = 0f };

            // With a non-positive radius this returns the current aspect ratio unchanged.
            Assert.AreEqual(cylinder.AspectRatio, cylinder.CalculateAspectRatioFromHeight(5f), k_Epsilon);
        }

        [Test]
        public void Cylinder_CalculateRadiusFromHeight_GuardsZeroCentralAngle()
        {
            var cylinder = new CylinderLayerData();
            cylinder.CentralAngle = 0f;

            // With a non-positive central angle this returns the current radius unchanged.
            Assert.AreEqual(cylinder.Radius, cylinder.CalculateRadiusFromHeight(5f), k_Epsilon);
        }

        // --- Equirect Mesh ---

        [Test]
        public void Equirect_Radius_ClampsNegativeToZero()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.Radius = -5f;

            Assert.AreEqual(0f, equirect.Radius, k_Epsilon);
        }

        [Test]
        public void Equirect_CentralHorizontalAngleInDegrees_RoundTrips()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.CentralHorizontalAngleInDegrees = 180f;

            Assert.AreEqual(180f, equirect.CentralHorizontalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(Mathf.PI, equirect.CentralHorizontalAngle, k_Epsilon);
        }

        [Test]
        public void Equirect_VerticalAngleInDegrees_RoundTrips()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.UpperVerticalAngleInDegrees = 45f;
            equirect.LowerVerticalAngleInDegrees = -30f;

            Assert.AreEqual(45f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(-30f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
        }
    }
}
