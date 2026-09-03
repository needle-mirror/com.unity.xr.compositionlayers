using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Tests;

namespace Unity.XR.CompositionLayers.Extensions.Tests
{
    /// <summary>
    /// Tests the <see cref="HDRTonemappingExtension"/> defaults and that mutating each of its properties
    /// reports a modification to the layer provider.
    /// </summary>
    class HDRTonemappingExtensionTests : CompositionLayerManagerTestBase
    {
        // Public so it can be used as a parameter type on the public [Test] method.
        public enum Property { ColorGamut, NitsForPaperWhite, MaxDisplayNits }

        [Test]
        public void Defaults_SRGB_And160Nits_TargetIsLayer()
        {
            var layer = CreateTrackedLayer();
            var ext = layer.gameObject.AddComponent<HDRTonemappingExtension>();

            Assert.AreEqual(ColorGamut.sRGB, ext.ColorGamut);
            Assert.AreEqual(160, ext.NitsForPaperWhite);
            Assert.AreEqual(160, ext.MaxDisplayNits);
            Assert.AreEqual(CompositionLayerExtension.ExtensionTarget.Layer, ext.Target);
        }

        [Test]
        public async Task ChangingProperty_ReportsModified_ThenSettles(
            [Values(Property.ColorGamut, Property.NitsForPaperWhite, Property.MaxDisplayNits)] Property property)
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            var ext = layer.gameObject.AddComponent<HDRTonemappingExtension>();
            await new WaitForUpdateCall(this);
            await new WaitForUpdateCall(this);

            MutateProperty(ext, property);

            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);

            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);
        }

        static void MutateProperty(HDRTonemappingExtension ext, Property property)
        {
            switch (property)
            {
                case Property.ColorGamut:
                    ext.ColorGamut = ColorGamut.DisplayP3;
                    break;
                case Property.NitsForPaperWhite:
                    ext.NitsForPaperWhite = 200;
                    break;
                case Property.MaxDisplayNits:
                    ext.MaxDisplayNits = 200;
                    break;
            }
        }
    }
}
