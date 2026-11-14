#if UNITY_RENDER_PIPELINES_UNIVERSAL
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Rendering.Universal;

namespace Unity.XR.CompositionLayers.Emulation
{
    /// <summary>
    /// Kept only so URP Renderer assets that still list this feature keep loading. It has done
    /// nothing since Composition Layers 1.0.0; remove it from the Renderer asset.
    /// </summary>
    [System.Obsolete("EmulationLayerUniversalScriptableRendererFeature does nothing as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Delete it from your URP Renderer asset.", true)]
    public partial class EmulationLayerUniversalScriptableRendererFeature : ScriptableRendererFeature
    {
        [AutoStaticsCleanup]
        static bool s_Logged;

        public override void Create()
        {
            if (s_Logged)
            {
                return;
            }

            s_Logged = true;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            UnityEngine.Debug.LogWarning("EmulationLayerUniversalScriptableRendererFeature does nothing as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Delete it from your URP Renderer asset.");
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
        }
    }
}
#endif
