#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;

namespace Unity.XR.CompositionLayers.Emulation
{
    [System.Obsolete("EmulationLayerUniversalScriptableRendererFeature is not supported now. Please delete this feature manually.", false)]
    public partial class EmulationLayerUniversalScriptableRendererFeature : ScriptableRendererFeature
    {
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        static bool s_logged;

        public override void Create()
        {
            if(!s_logged)
            {
                s_logged = true;
                UnityEngine.Debug.LogWarning("EmulationLayerUniversalScriptableRendererFeature is not supported now. Please delete this feature manually.");
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
        }
    }
}
#endif
