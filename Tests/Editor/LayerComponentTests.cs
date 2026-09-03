using NUnit.Framework;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class LayerComponentTests : CompositionLayerManagerTestBase
    {
        [Test]
        public void AddingLayerAddsExpectedExtensions()
        {
            var layer = CreateLayerGameObject();

            layer.AddSuggestedExtensions();
            var texExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNull(texExt);

            var layerId = typeof(CylinderLayerData).FullName;
            var layerData = CompositionLayerUtils.CreateLayerData(layerId);
            layer.ChangeLayerDataType(layerData);
            layer.AddSuggestedExtensions();

            texExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(texExt);
        }
    }
}
