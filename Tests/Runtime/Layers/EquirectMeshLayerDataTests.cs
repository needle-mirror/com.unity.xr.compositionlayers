using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Logic tests for the <see cref="EquirectMeshLayerData"/> serialization migration and vertical
    /// angle setters. These operate on plain instances with no GameObject, scene, or manager.
    /// </summary>
    class EquirectMeshLayerDataTests
    {
        const float k_DegreesEpsilon = 1e-3f;

        [Test]
        public void Migration_LegacyFullSphere_RemapsToFullSphere()
        {
            var equirect = new EquirectMeshLayerData(); // unversioned, stands in for a pre-rework asset
            equirect.UpperVerticalAngleInDegrees = 90f;
            equirect.LowerVerticalAngleInDegrees = 90f;

            equirect.OnAfterDeserialize();

            Assert.AreEqual(90f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(-90f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
        }

        [Test]
        public void Migration_LegacyNonDefaultAngles_RemapsVerticalAnglesAndPreservesCentral()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.CentralHorizontalAngleInDegrees = 180f;
            equirect.UpperVerticalAngleInDegrees = 0f;
            equirect.LowerVerticalAngleInDegrees = -90f;

            // Conversion: upper -> upper/2 + 45, lower -> -lower/2 - 45.
            equirect.OnAfterDeserialize();

            Assert.AreEqual(45f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(0f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(180f, equirect.CentralHorizontalAngleInDegrees, k_DegreesEpsilon);
        }

        [Test]
        public void Migration_AlreadyCurrentVersion_IsNotRemapped()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.UpperVerticalAngleInDegrees = 90f;
            equirect.LowerVerticalAngleInDegrees = 90f;
            equirect.OnBeforeSerialize(); // marks it as already at the current serialized version

            equirect.OnAfterDeserialize();

            Assert.AreEqual(90f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(90f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
        }

        [Test]
        public void Defaults_FullSphere_SurviveRoundTripUnchanged()
        {
            var equirect = new EquirectMeshLayerData();

            Assert.AreEqual(90f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(-90f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);

            var restored = new EquirectMeshLayerData();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(equirect), restored);

            Assert.AreEqual(90f, restored.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
            Assert.AreEqual(-90f, restored.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
        }

        [Test]
        public void UpperVerticalAngle_SetBelowLower_IsNotClamped()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.LowerVerticalAngleInDegrees = 0f;

            equirect.UpperVerticalAngleInDegrees = -45f;

            Assert.AreEqual(-45f, equirect.UpperVerticalAngleInDegrees, k_DegreesEpsilon);
        }

        [Test]
        public void LowerVerticalAngle_SetAboveUpper_IsNotClamped()
        {
            var equirect = new EquirectMeshLayerData();
            equirect.UpperVerticalAngleInDegrees = 0f;

            equirect.LowerVerticalAngleInDegrees = 45f;

            Assert.AreEqual(45f, equirect.LowerVerticalAngleInDegrees, k_DegreesEpsilon);
        }
    }
}
