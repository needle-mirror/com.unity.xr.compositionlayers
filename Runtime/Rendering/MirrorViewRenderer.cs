#if UNITY_EDITOR
#define SUPPORT_SETMIRRORBLITMODE_ONGUI
#endif

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using Unity.XR.CompositionLayers.Rendering.Internals;
using Unity.XR.CompositionLayers.Services;
using System.Collections;

namespace Unity.XR.CompositionLayers.Rendering
{
#pragma warning disable CS0618
    /// <summary>
    /// XR mirror view rendering on XR Composition Layers.
    /// This component works on URP. HDRP support is deprecated and will be removed in a future release.
    /// Call XRDisplaySystem.SetPreferredMirrorBlitMode() to set the mirror blit mode.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(32000)]
    public partial class MirrorViewRenderer : MonoBehaviour
    {
        /// <summary>
        /// How the mirror view blends with the back buffer. <see cref="ImageFilters.AlphaMode.Opaque"/> works in most cases.
        /// <see cref="ImageFilters.AlphaMode.Alpha"/> or <see cref="ImageFilters.AlphaMode.Premultiply"/> are used for overlay
        /// drawing and need an alpha channel on the back buffer.
        /// </summary>
        public ImageFilters.AlphaMode AlphaBlendMode
        {
            get => AlphaMode;
            set => AlphaMode = value;
        }

        Camera m_Camera;
        CommandBuffer m_CommandBuffer;
        bool m_IsAddedCommandBufferToCamera;
        XRDisplaySubsystem m_DisplaySubsystem;
        int m_MirrorBlitMode;

        ImageFilters.RenderPipeline m_RenderPipeline = ImageFilters.RenderPipeline.Universal;
        bool m_WarnedUnsupportedRenderPipeline;

        void Start()
        {
            UpdateMirrorView();

            StartCoroutine(PostfixMirrorBlitMode());
        }

        IEnumerator PostfixMirrorBlitMode()
        {
            for (; ; )
            {
                yield return new WaitForEndOfFrame();
#if !SUPPORT_SETMIRRORBLITMODE_ONGUI
                if (m_DisplaySubsystem != null && m_DisplaySubsystem.running)
                {
                    m_DisplaySubsystem.SetPreferredMirrorBlitMode(m_MirrorBlitMode);
                }
#endif
            }
        }

        void LateUpdate()
        {
            UpdateMirrorView();
            UpdateCommandBuffer();

#if !SUPPORT_SETMIRRORBLITMODE_ONGUI
            if (m_DisplaySubsystem != null)
            {
                m_DisplaySubsystem.SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.None);
            }
#endif
        }

        void OnDestroy()
        {
            CleanupMirrorView();

            m_Camera = null;
            m_CommandBuffer = null;
            m_DisplaySubsystem = null;
        }

        void OnEnable()
        {
            UpdateMirrorView();
            UpdateCommandBuffer();

            if (m_DisplaySubsystem == null)
            {
                if (XRHelper.GetActiveDisplaySubsystem() == null)
                {
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning("MirrorViewRenderer works with XR mode only.");
                    }
                }
            }
        }

        void OnDisable()
        {
            CleanupMirrorView();
        }

        void UpdateMirrorView()
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
                if (m_Camera != null)
                {
                    m_Camera.clearFlags = CameraClearFlags.Nothing;
                    if (GraphicsSettings.currentRenderPipeline != null)
                    {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
                        var universalAdditionalCameraData = m_Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                        if (universalAdditionalCameraData != null)
                        {
                            universalAdditionalCameraData.allowXRRendering = false;
                            if (RenderPipelineUtility.IsUniversalRenderPipeline())
                            {
                                m_RenderPipeline = ImageFilters.RenderPipeline.Universal;
                            }
                        }
#endif
#if UNITY_RENDER_PIPELINES_HDRENDER
                        var hdAdditionalCameraData = m_Camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                        if (hdAdditionalCameraData != null)
                        {
                            hdAdditionalCameraData.xrRendering = false;
                            if (RenderPipelineUtility.IsHDRenderPipeline())
                            {
                                m_RenderPipeline = ImageFilters.RenderPipeline.HighDefinition;
#if UNITY_EDITOR
                                Debug.LogWarning("High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.");
#endif
                            }
                        }
#endif
                    }

                    m_Camera.cullingMask = 0;
                }
            }

            var displaySystem = XRHelper.GetActiveDisplaySubsystem();
            if (displaySystem == null)
            {
                CleanupMirrorView();
                return;
            }

            var blitMode = displaySystem.GetPreferredMirrorBlitMode();
            if (m_DisplaySubsystem == null)
            {
                if (blitMode == XRMirrorViewBlitMode.None)
                {
                    CleanupMirrorView();
                    return;
                }

                m_MirrorBlitMode = blitMode;
            }
            else
            {
#if SUPPORT_SETMIRRORBLITMODE_ONGUI
                m_MirrorBlitMode = blitMode;
#else
                if (blitMode != XRMirrorViewBlitMode.None)
                {
                    m_MirrorBlitMode = blitMode;
                }
#endif
            }

            if (m_DisplaySubsystem == displaySystem)
            {
                return;
            }

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                if (!m_WarnedUnsupportedRenderPipeline)
                {
                    m_WarnedUnsupportedRenderPipeline = true;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
                    Debug.LogWarning("MirrorViewRenderer requires the Universal Render Pipeline.");
#endif
                }

                CleanupMirrorView();
                return;
            }

            m_DisplaySubsystem = displaySystem;

            AddCommandBufferToCamera();

            if (m_Camera != null)
            {
                m_Camera.clearFlags = CameraClearFlags.SolidColor;
                m_Camera.backgroundColor = Color.black;
                m_Camera.enabled = true;
            }
        }

        void CleanupMirrorView()
        {
            if (m_Camera != null)
            {
                m_Camera.clearFlags = CameraClearFlags.Nothing;
                m_Camera.enabled = false;
            }

            RemoveCommandBufferFromCamera();

            if (m_DisplaySubsystem != null)
            {
                if (m_DisplaySubsystem == XRHelper.GetActiveDisplaySubsystem())
                {
                    m_DisplaySubsystem.SetPreferredMirrorBlitMode(m_MirrorBlitMode);
                }

                m_DisplaySubsystem = null;
            }
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == m_Camera)
            {
                if (m_RenderPipeline == ImageFilters.RenderPipeline.Universal)
                    Graphics.ExecuteCommandBuffer(m_CommandBuffer);
                else
                    context.ExecuteCommandBuffer(m_CommandBuffer);
            }
        }

        void AddCommandBufferToCamera()
        {
            if (m_Camera != null && !m_IsAddedCommandBufferToCamera)
            {
                m_CommandBuffer = new CommandBuffer();
                m_CommandBuffer.name = "MirrorViewRenderer";
                m_IsAddedCommandBufferToCamera = true;
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            }
        }

        void RemoveCommandBufferFromCamera()
        {
            if (m_Camera != null && m_IsAddedCommandBufferToCamera)
            {
                m_IsAddedCommandBufferToCamera = false;
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
                m_CommandBuffer = null;
            }
        }

        void UpdateCommandBuffer()
        {
            if (m_CommandBuffer == null)
            {
                return;
            }

            m_CommandBuffer.Clear();

            var mirrorViewParams = new ImageFilters.MirrorViewParams();
            mirrorViewParams.displaySubsystem = m_DisplaySubsystem;
            mirrorViewParams.mainCamera = CompositionLayerUtils.GetStereoMainCamera();
            mirrorViewParams.mirrorViewCamera = m_Camera;
            mirrorViewParams.blitMode = m_MirrorBlitMode == XRMirrorViewBlitMode.Default ? XRMirrorViewBlitMode.SideBySide : m_MirrorBlitMode;
            mirrorViewParams.alphaMode = AlphaBlendMode;
            mirrorViewParams.renderPipeline = m_RenderPipeline;

            ImageFilters.BlitMirrorView(m_CommandBuffer, mirrorViewParams);
        }
    }
#pragma warning restore CS0618
}
