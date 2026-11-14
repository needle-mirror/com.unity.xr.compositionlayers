using System;

namespace Unity.XR.CompositionLayers.Layers
{
    public sealed partial class CompositionLayerDataAttribute
    {
        /// <summary>
        /// Obsolete. Use <see cref="SuggestedExtensionTypes"/> instead.
        /// </summary>
        [Obsolete("SuggestedExtenstionTypes is obsolete as of Composition Layers 7.0.0-pre.1. Use SuggestedExtensionTypes instead. The field will be removed in a future release.")]
        public Type[] SuggestedExtenstionTypes = Type.EmptyTypes;
    }
}
