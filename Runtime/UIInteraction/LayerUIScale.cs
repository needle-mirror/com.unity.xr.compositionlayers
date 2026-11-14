using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Extensions;

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Base class for handling quad / cylinder collider generation and maintaining aspect ratio with Canvas changes
    /// </summary>
    [ExecuteInEditMode]
    public partial class LayerUIScale : MonoBehaviour
    {
#pragma warning disable CS0618
        /// <summary>
        /// Reference to TexturesExtension for cropping and stretching
        /// </summary>
        protected TexturesExtension TexturesExtension
        {
            get => texturesExtension;
            set => texturesExtension = value;
        }

        /// <summary>
        /// Reference to RectTransform to check for size or scale changes
        /// </summary>
        protected RectTransform CanvasRect
        {
            get => canvasRect;
            set => canvasRect = value;
        }

        /// <summary>
        /// Cached RectTransform's sizeDelta.x
        /// </summary>
        protected float CanvasSizeX
        {
            get => canvasSizeX;
            set => canvasSizeX = value;
        }

        /// <summary>
        /// Cached RectTransform's sizeDelta.y
        /// </summary>
        protected float CanvasSizeY
        {
            get => canvasSizeY;
            set => canvasSizeY = value;
        }

        /// <summary>
        /// Cached RectTransform's localScale.x
        /// </summary>
        protected float CanvasScaleX
        {
            get => canvasScaleX;
            set => canvasScaleX = value;
        }

        /// <summary>
        /// Cached RectTransform's localScale.y
        /// </summary>
        protected float CanvasScaleY
        {
            get => canvasScaleY;
            set => canvasScaleY = value;
        }
#pragma warning restore CS0618

        /// <summary>
        /// Scalar of height for transform recalculations
        /// </summary>
        public float HeightScale;

        /// <summary>
        /// Scalar of width for transform recalculations
        /// </summary>
        public float WidthScale;

        /// <summary>
        /// Calculates the HeightScale and WidthScale based on the composition layer's aspect, the canvas size, and canvas scale.
        /// </summary>
        /// <param name="aspect">Aspect ratio of Layer</param>
        public void UpdateDestinationRectScale(float aspect)
        {
            if (TexturesExtension == null)
            {
                if (!TryGetComponent<TexturesExtension>(out var extension))
                {
                    return;
                }

                TexturesExtension = extension;
            }

            TexturesExtension.CustomRects = true;

            // Canvas Aspect Ratio
            var canvasScaleAspect = CanvasScaleX / CanvasScaleY;
            var canvasAspect = (CanvasSizeX / CanvasSizeY) * canvasScaleAspect;

            // Set destination rect values based on the canvas vs cylinder aspect ratio
            bool canvasAspectLarger = canvasAspect > aspect;

            // Calculate destination rect H and Y value to maintain aspect ratio
            var H = canvasAspectLarger ? aspect / canvasAspect : 1.0f;
            float Y = canvasAspectLarger ? (1.0f - H) / 2.0f : 0.0f;

            // Calculate destination rect W and X value to maintain aspect ratio
            var W = !canvasAspectLarger ? canvasAspect / aspect : 1.0f;
            float X = !canvasAspectLarger ? (1.0f - W) / 2.0f : 0.0f;

            // Apply Width, Height, X, and Y to source and destination rects
            TexturesExtension.LeftEyeDestinationRect = new Rect(X, Y, W, H);

            HeightScale = H;
            WidthScale = W;
        }

        /// <summary>
        /// Check whether or not the canvas has changed in size or scale.
        /// </summary>
        /// <returns>Whether or not the canvas rect has been changed (size or scale)</returns>
        protected bool CanvasAdjusted()
        {
            if (CanvasRect == null)
            {
                if (!transform.GetChild(0).TryGetComponent<RectTransform>(out var rect))
                {
                    return false;
                }

                CanvasRect = rect;
            }

            if (CanvasSizeX != CanvasRect.sizeDelta.x || CanvasSizeY != CanvasRect.sizeDelta.y
                || CanvasScaleX != CanvasRect.localScale.x || CanvasScaleY != CanvasRect.localScale.y)
            {
                CanvasSizeX = CanvasRect.sizeDelta.x;
                CanvasSizeY = CanvasRect.sizeDelta.y;
                CanvasScaleX = CanvasRect.localScale.x;
                CanvasScaleY = CanvasRect.localScale.y;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates a Vector3 scalar to help convert a point from world space to the local space.
        /// Mainly used for the UIHandle positioning.
        /// </summary>
        /// <returns>Returns a Vector3 scalar to transform a point on a Canvas to a point on the Layer</returns>
        public virtual Vector3 GetUIScale() => Vector3.one;

        /// <summary>
        /// Inverts the UI Scale from GetUIScale for inverse scaling
        /// Mainly used for the UIHandle positioning
        /// </summary>
        /// <returns>Returns a Vector3 scalar to transform a point on a Canvas to a point on the Layer</returns>
        /// <seealso cref="GetUIScale"/>
        public Vector3 GetInverseUIScale() => new Vector3(1f / GetUIScale().x, 1f / GetUIScale().y, 1f / GetUIScale().z);
    }
}
