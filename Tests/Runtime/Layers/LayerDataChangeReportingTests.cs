using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Tests that mutating a <see cref="LayerData"/> property on an active layer reports a
    /// modification to the layer provider through <see cref="LayerData.ReportStateChange"/>
    /// (and that setting an unchanged value does not).
    /// </summary>
    [Category("Level.Unit")]
    [NUnit.Framework.Property("Covers", "CompositionLayers")]
    class LayerDataChangeReportingTests : CompositionLayerManagerTestBase
    {
        const float k_ChangedValue = 2.5f;

        [Test]
        [Description("Setting a LayerData property to a new value on an active layer reports that layer " +
                     "and setting the same value again reports nothing.")]
        public async Task ChangingLayerDataProperty_ReportsModified_ThenSettles([Values] bool useRadius)
        {
            var layer = CreateActiveLayer<CylinderLayerData>();
            var layerData = (CylinderLayerData)layer.LayerData;
            await NextManagerUpdate();
            await NextManagerUpdate(); // settle the create

            SetProperty(layerData, useRadius);

            await NextManagerUpdate();
            Assert.IsTrue(hasModifiedLayers);
            Assert.IsTrue(WasReportedModified(layer));

            await NextManagerUpdate();
            Assert.IsFalse(hasModifiedLayers);

            // Setting the same value again should not report a modification.
            SetProperty(layerData, useRadius);

            await NextManagerUpdate();
            Assert.IsFalse(hasModifiedLayers);
        }

        static void SetProperty(CylinderLayerData layerData, bool useRadius)
        {
            if (useRadius)
            {
                layerData.Radius = k_ChangedValue;
            }
            else
            {
                layerData.CentralAngle = k_ChangedValue;
            }
        }
    }
}
