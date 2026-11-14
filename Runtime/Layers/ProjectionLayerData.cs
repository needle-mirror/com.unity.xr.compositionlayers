using System;
using Unity.XR.CompositionLayers.Extensions;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Layers
{
    /// <summary>
    /// Projection layer type for full screen rendering of a texture composition layer.
    /// </summary>
    [Serializable]
    [CompositionLayerData(
        Provider = "Unity",
        Name = "Projection",
        IconPath = "",
        InspectorIcon = "",
        ListViewIcon = "",
        Description = "Projection Layer",
        SuggestedExtensionTypes = new[] { typeof(TexturesExtension) }
     )]
    public class ProjectionLayerData : LayerData { }
}
