using System;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;

namespace Unity.XR.CompositionLayers.UIInteraction
{
    public partial class LayerUIScale
    {
        /// <summary>
        /// Obsolete. Use <see cref="TexturesExtension"/> instead.
        /// </summary>
        [Obsolete("texturesExtension is obsolete as of Composition Layers 7.0.0-pre.1. Use TexturesExtension instead. The field will be removed in a future release.")]
        protected TexturesExtension texturesExtension;

        /// <summary>
        /// Obsolete. Use <see cref="CanvasRect"/> instead.
        /// </summary>
        [Obsolete("canvasRect is obsolete as of Composition Layers 7.0.0-pre.1. Use CanvasRect instead. The field will be removed in a future release.")]
        protected RectTransform canvasRect;

        /// <summary>
        /// Obsolete. Use <see cref="CanvasSizeX"/> instead.
        /// </summary>
        [Obsolete("canvasSizeX is obsolete as of Composition Layers 7.0.0-pre.1. Use CanvasSizeX instead. The field will be removed in a future release.")]
        protected float canvasSizeX = -1;

        /// <summary>
        /// Obsolete. Use <see cref="CanvasSizeY"/> instead.
        /// </summary>
        [Obsolete("canvasSizeY is obsolete as of Composition Layers 7.0.0-pre.1. Use CanvasSizeY instead. The field will be removed in a future release.")]
        protected float canvasSizeY = -1;

        /// <summary>
        /// Obsolete. Use <see cref="CanvasScaleX"/> instead.
        /// </summary>
        [Obsolete("canvasScaleX is obsolete as of Composition Layers 7.0.0-pre.1. Use CanvasScaleX instead. The field will be removed in a future release.")]
        protected float canvasScaleX = -1;

        /// <summary>
        /// Obsolete. Use <see cref="CanvasScaleY"/> instead.
        /// </summary>
        [Obsolete("canvasScaleY is obsolete as of Composition Layers 7.0.0-pre.1. Use CanvasScaleY instead. The field will be removed in a future release.")]
        protected float canvasScaleY = -1;
    }
}
