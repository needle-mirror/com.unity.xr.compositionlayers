using System;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Rendering
{
    public partial class MirrorViewRenderer
    {
        /// <summary>
        /// Obsolete. Use <see cref="AlphaBlendMode"/> instead.
        /// </summary>
        [Obsolete("AlphaMode is obsolete as of Composition Layers 7.0.0-pre.1. Use AlphaBlendMode instead. The field will be removed in a future release.")]
        public ImageFilters.AlphaMode AlphaMode = ImageFilters.AlphaMode.Opaque;
    }
}
