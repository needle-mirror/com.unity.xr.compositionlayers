using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Rendering
{
    public static class RenderPipelineUtility
    {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
        public static bool IsUniversalRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return false;
#if UNITY_2023_2_OR_NEWER
            return typeof(UnityEngine.Rendering.Universal.UniversalRenderPipeline).IsAssignableFrom(GraphicsSettings.currentRenderPipeline.pipelineType);
#else
            return GraphicsSettings.currentRenderPipeline.renderPipelineShaderTag == "UniversalPipeline";
#endif
        }
#endif

#if UNITY_RENDER_PIPELINES_HDRENDER
        public static bool IsHDRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return false;
#if UNITY_2023_2_OR_NEWER
            return typeof(UnityEngine.Rendering.HighDefinition.HDRenderPipeline).IsAssignableFrom(GraphicsSettings.currentRenderPipeline.pipelineType);
#else
            return GraphicsSettings.currentRenderPipeline.renderPipelineShaderTag == "HDRenderPipeline";
#endif
        }
#endif
    }
}
