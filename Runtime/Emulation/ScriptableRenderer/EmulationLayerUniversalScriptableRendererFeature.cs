#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;

namespace Unity.XR.CompositionLayers.Emulation
{
    public class EmulationLayerUniversalScriptableRendererFeature : ScriptableRendererFeature
    {
        EmulationLayerUniversalScriptableRendererPass m_UnderlayLayerPass;
        EmulationLayerUniversalScriptableRendererPass m_OverlayLayerPass;

        public override void Create()
        {
            // Pass instances should be created for each PassEvent.(EnqueuePass will keep instances for each pass.)
            m_UnderlayLayerPass = new EmulationLayerUniversalScriptableRendererPass(false);
            m_OverlayLayerPass = new EmulationLayerUniversalScriptableRendererPass(true);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_UnderlayLayerPass);
            renderer.EnqueuePass(m_OverlayLayerPass);
        }
    }
}
#endif
