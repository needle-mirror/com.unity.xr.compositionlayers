using System;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CoreUtils;

namespace Unity.XR.CompositionLayers.Layers
{
    /// <summary>
    /// Subclass of <see cref="LayerData" /> that represents the default base
    /// rendered layer for composition layer ordering. This is an implicit 
    /// layer that Unity will render to the display of the target XR device.
    ///
    /// The intention of this layer is to provide a default "invisible" layer 
    /// to act as the 0th layer which seperates underlay layers from overlay layers.
    /// </summary>
    [CompositionLayerData(
        Provider = "Unity",
        Name = "Default Scene",
        IconPath = CompositionLayerConstants.IconPath,
        InspectorIcon = "",
        ListViewIcon = "",
        Description = "Represents the default base composition layer. This is an implicit layer which seperates the overlay layers from the underlays.",
        SuggestedExtenstionTypes = new Type[] { }
     )]
    [CompositionLayersHelpURL(typeof(DefaultLayerData))]
    public class DefaultLayerData : LayerData {
        [UnityEngine.Scripting.Preserve]

        public DefaultLayerData()
        {
        }

        protected internal override bool Validate(CompositionLayer layer)
        {
            var layerManager = CompositionLayerManager.Instance;
            if (layerManager == null)
                return false;

            layerManager.DefaultSceneCompositionLayer = layer;
            var isDefaultSceneLayer = layerManager.DefaultSceneCompositionLayer == layer;
            layer.gameObject.SetActive(isDefaultSceneLayer);
            return isDefaultSceneLayer;
        }
    }
}
