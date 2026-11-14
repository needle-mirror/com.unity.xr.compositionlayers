using Unity.Scripting.LifecycleManagement;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.XR.CompositionLayers.Rendering.Internals;
using Unity.XR.CompositionLayers.Services;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Unity.XR.CompositionLayers.Emulation
{
    /// <summary>
    /// The custom render pass that applies the Color Scale and Bias effect.
    /// </summary>
    internal partial class EmulationColorScaleBiasPass : ScriptableRenderPass
    {
        const string k_PassName = "EmulatedColorScaleBiasPass";
        const string k_ShaderName = "Hidden/XRCompositionLayers/ColorScaleBias";
        const RenderPassEvent k_ColorScaleBiasRenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        static readonly int k_ScaleParameterName = Shader.PropertyToID("_ColorScale");
        static readonly int k_BiasParameterName = Shader.PropertyToID("_ColorBias");
        [NoAutoStaticsCleanup]
        static bool s_InjectPassRegistered;
        [AutoStaticsCleanup]
        static EmulationColorScaleBiasPass s_ColorScaleBiasPass;

        Material m_EmulationMaterial;


        /// <summary>
        /// Constructor to initialize the render pass with the appropriate render event.
        /// </summary>
        internal EmulationColorScaleBiasPass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            CreateEmulationMaterial();
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Creates the material used for emulating color scale and bias.
        /// </summary>
        void CreateEmulationMaterial()
        {
            Shader shader = Shader.Find(k_ShaderName);
            if (shader != null)
            {
                m_EmulationMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                m_EmulationMaterial.SetVector(k_ScaleParameterName, Vector4.one);
                m_EmulationMaterial.SetVector(k_BiasParameterName, Vector4.zero);
            }
        }

        /// <summary>
        /// Checks if the ColorScaleBias extension exists in the current scene.
        /// </summary>
        /// <returns> Returns ture if compoenet is attached to Default Layer else false.</returns>
        bool IsExtensionInScene(Camera camera)
        {
            if (m_EmulationMaterial == null)
            {
                return false;
            }

            CompositionLayerManager compositionLayerManager = CompositionLayerManager.Instance;
            if (compositionLayerManager == null)
            {
                return false;
            }

            var defaultLayer = compositionLayerManager.DefaultSceneCompositionLayer;
            var colorScaleBiasExtension =
                EmulatedCompositionLayerUtils.GetEmulatedColorScaleBias(defaultLayer, camera.cameraType);
            if (colorScaleBiasExtension == null)
            {
                return false;
            }

            // Apply the scale and bias values from the extension to the material
            m_EmulationMaterial.SetVector(k_ScaleParameterName, colorScaleBiasExtension.Scale);
            m_EmulationMaterial.SetVector(k_BiasParameterName, colorScaleBiasExtension.Bias);

            return true;
        }

        /// <summary>
        /// Handles the execution of the render pass in RenderGraph mode.
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var camera = cameraData.camera;
            if (resourceData.isActiveTargetBackBuffer || !IsSupported(camera) || !IsExtensionInScene(camera))
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = k_PassName;
            destinationDesc.clearBuffer = true;
            destinationDesc.clearColor = Color.clear;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_EmulationMaterial, 0);
            renderGraph.AddBlitPass(para, k_PassName);

            resourceData.cameraColor = destination;

        }

        /// <summary>
        /// Determines whether the given camera supports the effect.
        /// </summary>
        static bool IsSupported(Camera camera) => EmulatedLayerProvider.IsSupported(camera);

        /// <summary>
        /// Registers the scriptable render pass to be injected into the render pipeline.
        /// </summary>
        internal static void RegisterScriptableRendererPass()
        {
            if (!s_InjectPassRegistered)
            {
                s_InjectPassRegistered = true;
                RenderPipelineManager.beginCameraRendering += InjectPass;
            }
        }

        /// <summary>
        /// Unregisters the scriptable render pass from the render pipeline.
        /// </summary>
        internal static void UnregisterScriptableRendererPass()
        {
            if (s_InjectPassRegistered)
            {
                s_InjectPassRegistered = false;
                RenderPipelineManager.beginCameraRendering -= InjectPass;
            }
        }

        /// <summary>
        /// Injects the render pass into the camera rendering process.
        /// </summary>
        static void InjectPass(ScriptableRenderContext renderContext, Camera currCamera)
        {
            var data = currCamera.GetUniversalAdditionalCameraData();
            if (data != null && data.scriptableRenderer != null)
            {
#pragma warning disable CS0618
                data.scriptableRenderer.EnqueuePass(ColorScaleBiasPass);
#pragma warning restore CS0618
            }
        }

        /// <summary>
        /// Retrieves or creates an instance of the color scale bias pass.
        /// </summary>
        static EmulationColorScaleBiasPass ColorScaleBiasPass
        {
            get
            {
                if (s_ColorScaleBiasPass == null)
                {
                    s_ColorScaleBiasPass = new EmulationColorScaleBiasPass(k_ColorScaleBiasRenderPassEvent);
                }

                return s_ColorScaleBiasPass;
            }
        }
    }
}
#endif
