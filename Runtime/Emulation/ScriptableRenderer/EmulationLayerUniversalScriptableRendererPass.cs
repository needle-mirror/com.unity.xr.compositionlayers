#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.XR.CompositionLayers.Emulation
{
    public class EmulationLayerUniversalScriptableRendererPass : ScriptableRenderPass
    {
        const RenderPassEvent UnderlayRenderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
        const RenderPassEvent OverlayRenderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        bool m_IsOverray;

        public EmulationLayerUniversalScriptableRendererPass(bool isOverray)
        {
            m_IsOverray = isOverray;
            renderPassEvent = isOverray ? OverlayRenderPassEvent : UnderlayRenderPassEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var commandBuffers = EmulationLayerUniversalScriptableRendererManager.GetCommandBuffers(camera, m_IsOverray);
            if (commandBuffers != null)
            {
                foreach (var commandBuffer in commandBuffers)
                {
                    context.ExecuteCommandBuffer(commandBuffer);
                }
            }
        }
    }
}
#endif
