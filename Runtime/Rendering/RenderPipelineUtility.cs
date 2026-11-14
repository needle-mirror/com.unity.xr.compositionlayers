using System;
using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Rendering
{
    /// <summary>
    /// Provides utility methods for determining the active render pipeline.
    /// </summary>
    public static class RenderPipelineUtility
    {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
        /// <summary>
        /// Checks if the current render pipeline is the Universal Render Pipeline (URP).
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the current render pipeline is URP. Otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsUniversalRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return false;

            return typeof(UnityEngine.Rendering.Universal.UniversalRenderPipeline).IsAssignableFrom(GraphicsSettings.currentRenderPipeline.pipelineType);
        }
#endif

#if UNITY_RENDER_PIPELINES_HDRENDER
        /// <summary>
        /// Checks if the current render pipeline is the High Definition Render Pipeline (HDRP).
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the current render pipeline is HDRP. Otherwise, <see langword="false"/>.
        /// </returns>
        [Obsolete("High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.")]
        public static bool IsHDRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return false;

            return typeof(UnityEngine.Rendering.HighDefinition.HDRenderPipeline).IsAssignableFrom(GraphicsSettings.currentRenderPipeline.pipelineType);
        }
#endif
    }
}
