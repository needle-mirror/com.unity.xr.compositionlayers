using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Tests;

namespace Unity.XR.CompositionLayers.Extensions.Tests
{
    /// <summary>
    /// Tests the <see cref="ColorScaleBiasExtension"/> defaults and that mutating Scale/Bias reports a
    /// modification to the layer provider (and that setting an unchanged value does not).
    /// </summary>
    class ColorScaleBiasExtensionTests : CompositionLayerManagerTestBase
    {
        [Test]
        public void Defaults_ScaleIsOne_BiasIsZero_TargetIsLayer()
        {
            var layer = CreateTrackedLayer();
            var ext = layer.gameObject.AddComponent<ColorScaleBiasExtension>();

            Assert.AreEqual(Vector4.one, ext.Scale);
            Assert.AreEqual(Vector4.zero, ext.Bias);
            Assert.AreEqual(CompositionLayerExtension.ExtensionTarget.Layer, ext.Target);
        }

        [Test]
        public async Task ChangingScaleOrBias_ReportsModified_ThenSettles([Values] bool useScale)
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            var ext = layer.gameObject.AddComponent<ColorScaleBiasExtension>();
            await new WaitForUpdateCall(this);
            await new WaitForUpdateCall(this); // settle the add

            var changed = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
            if (useScale)
                ext.Scale = changed;
            else
                ext.Bias = changed;

            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);

            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            // Setting the same value again should not report a modification.
            if (useScale)
                ext.Scale = changed;
            else
                ext.Bias = changed;

            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);
        }
    }
}
