using System;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Unity.XR.CoreUtils;
using Unity.XR.CompositionLayers.Services;

namespace Unity.XR.CompositionLayers.Emulation.Implementations
{
    /// <summary>
    /// Base for emulating <see cref="ProjectionLayerRigData"/>. Used to emulate a full screen texture rendering to the compositor.
    /// </summary>
    [EmulatedLayerDataType(typeof(ProjectionLayerRigData))]
    internal class ProjectionRigEmulationLayerData : EmulatedMeshLayerData
    {
        public const string k_ShaderLayerTypeKeyword = "COMPOSITION_LAYERTYPE_PROJECTION";

        Camera MainCamera => CompositionLayerManager.MainCameraCache;

        // Caches the last supported camera so this object knows which camera it's preparing command buffers for.
        Camera currentSupportedCamera;

        // Caches cameras and textures for left and right eyes
        internal Camera leftCam;
        internal Camera rightCam;
        RenderTexture m_emulationLeftEyeTexture;
        RenderTexture m_emulationRightEyeTexture;
        bool m_MissingCamerasReported;

        int m_CachedLayerId;
        bool m_EyeProjectionsApplied;
        bool m_EyeProjectionsAvailable;

        public ProjectionRigEmulationLayerData()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected internal override void UpdateEmulatedLayerData()
        {
            base.UpdateEmulatedLayerData();

            if (MainCamera == null)
                return;

            if (NeedsEyeTextures())
                CreateAndSetRenderTexture(MainCamera.pixelWidth, MainCamera.pixelHeight, 32);

            if (leftCam == null || rightCam == null)
                return;

            if (!Application.isPlaying)
            {
                // Ensure that the projection rig cameras have the same local pose offset as the main camera.
                var parent = leftCam.transform.parent;
                if (parent != null)
                {
                    parent.SetWorldPose(GetTotalLocalPoseOffset(MainCamera.transform));
                    leftCam.transform.SetLocalPose(Pose.identity);
                    rightCam.transform.SetLocalPose(Pose.identity);
                }
            }

            if (!Application.isPlaying)
                return;

            if (!CompositionLayerUtils.IsDisplaySubsystemActive())
            {
                leftCam.transform.SetPositionAndRotation(MainCamera.transform.position, MainCamera.transform.rotation);
                rightCam.transform.SetPositionAndRotation(MainCamera.transform.position, MainCamera.transform.rotation);
                ResetEyeProjections();
            }
            else if (IsEmulated && m_EyeProjectionsAvailable)
            {
                CopyClipPlanes(MainCamera, leftCam, rightCam);
                ApplyEyeProjections(
                    leftCam,
                    rightCam,
                    MainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left),
                    MainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
                m_EyeProjectionsApplied = true;
            }
        }

        // A camera that is deleted or moved out of the rig releases the textures and reports it; a resized Game view
        // needs textures of the new size.
        bool NeedsEyeTextures()
        {
            return !IsUnderRig(leftCam)
                || !IsUnderRig(rightCam)
                || m_emulationLeftEyeTexture == null
                || m_emulationRightEyeTexture == null
                || m_emulationLeftEyeTexture.width != MainCamera.pixelWidth
                || m_emulationLeftEyeTexture.height != MainCamera.pixelHeight;
        }

        bool IsUnderRig(Camera camera)
        {
            return camera != null && camera.transform.IsChildOf(CompositionLayer.transform);
        }

        internal static void ApplyEyeProjections(
            Camera leftCamera, Camera rightCamera, Matrix4x4 leftProjection, Matrix4x4 rightProjection)
        {
            leftCamera.projectionMatrix = leftProjection;
            rightCamera.projectionMatrix = rightProjection;
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!Application.isPlaying || camera != MainCamera || !camera.stereoEnabled)
                return;

            m_EyeProjectionsAvailable = true;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        internal static void CopyClipPlanes(Camera source, Camera leftCamera, Camera rightCamera)
        {
            leftCamera.nearClipPlane = source.nearClipPlane;
            leftCamera.farClipPlane = source.farClipPlane;
            rightCamera.nearClipPlane = source.nearClipPlane;
            rightCamera.farClipPlane = source.farClipPlane;
        }

        void ResetEyeProjections()
        {
            if (!m_EyeProjectionsApplied)
                return;

            leftCam.ResetProjectionMatrix();
            rightCam.ResetProjectionMatrix();
            m_EyeProjectionsApplied = false;
        }

        private Pose GetTotalLocalPoseOffset(Transform currentTransform)
        {
            var totalLocalPoseOffset = currentTransform.GetLocalPose();

            while (currentTransform.parent != null)
            {
                var parentLocalPose = currentTransform.parent.GetLocalPose();
                totalLocalPoseOffset = new Pose(totalLocalPoseOffset.position + parentLocalPose.position, totalLocalPoseOffset.rotation * parentLocalPose.rotation);
                currentTransform = currentTransform.parent;
            }

            return totalLocalPoseOffset;
        }

        private void CreateAndSetRenderTexture(int width, int height, int depth)
        {
            var layer = CompositionLayer;
            if (!CompositionLayerManager.TryGetLayerId(layer, out m_CachedLayerId))
            {
                Debug.LogError("Failed to get layer id for projection rig emulation.");
                return;
            }

            ReleaseEyeTextures();
            if (!TryResolveCameras(layer))
                return;

            m_emulationLeftEyeTexture = CreateEyeTexture(layer.name + "_left", width, height, depth);
            m_emulationRightEyeTexture = CreateEyeTexture(layer.name + "_right", width, height, depth);

            var texturesExtension = layer.GetComponent<TexturesExtension>();
            if (texturesExtension != null)
            {
                texturesExtension.LeftTexture = m_emulationLeftEyeTexture;
                texturesExtension.RightTexture = m_emulationRightEyeTexture;
            }

            leftCam.targetTexture = m_emulationLeftEyeTexture;
            rightCam.targetTexture = m_emulationRightEyeTexture;
        }

        bool TryResolveCameras(CompositionLayer layer)
        {
            var rigData = layer.LayerData as ProjectionLayerRigData;
            if (rigData != null && rigData.TryGetCameras(layer.transform, out leftCam, out rightCam))
                return true;

            leftCam = null;
            rightCam = null;
            if (!m_MissingCamerasReported)
            {
                Debug.LogError($"{layer.name}: {ProjectionLayerRigData.k_MissingCamerasMessage}", layer);
                m_MissingCamerasReported = true;
            }

            return false;
        }

        static RenderTexture CreateEyeTexture(string name, int width, int height, int depth)
        {
            var texture = new RenderTexture(width, height, depth)
            {
                name = name,
                format = RenderTextureFormat.ARGB32,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat
            };
            texture.Create();
            return texture;
        }

        // Clears only references to this object's textures, which another provider may have replaced.
        void ReleaseEyeTextures()
        {
            if (leftCam != null && leftCam.targetTexture == m_emulationLeftEyeTexture)
                leftCam.targetTexture = null;

            if (rightCam != null && rightCam.targetTexture == m_emulationRightEyeTexture)
                rightCam.targetTexture = null;

            var layer = CompositionLayer;
            var texturesExtension = layer != null ? layer.GetComponent<TexturesExtension>() : null;
            if (texturesExtension != null)
            {
                if (texturesExtension.LeftTexture == m_emulationLeftEyeTexture)
                    texturesExtension.LeftTexture = null;

                if (texturesExtension.RightTexture == m_emulationRightEyeTexture)
                    texturesExtension.RightTexture = null;
            }

            ReleaseTexture(ref m_emulationLeftEyeTexture);
            ReleaseTexture(ref m_emulationRightEyeTexture);
        }

        static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }

        /// <summary>
        /// Releases what emulation created. The rig's cameras and components belong to the scene and stay; changing
        /// the layer type removes them instead.
        /// </summary>
        public override void Dispose()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            ReleaseEyeTextures();
#if UNITY_EDITOR
            SceneEmulatedProjectionRig.DestroyAllEmulatedRigsForLayerId(m_CachedLayerId);
#endif
            base.Dispose();
        }

        protected override void PrepareCommands()
        {
            if (currentSupportedCamera == null || currentSupportedCamera.cameraType == CameraType.SceneView)
                return;

            base.PrepareCommands();
        }

        protected override void AddCommands(RenderContext renderContext, CommandArgs commandArgs)
        {
            if (currentSupportedCamera == null)
                return;
            if (currentSupportedCamera.cameraType == CameraType.SceneView)
            {
                SetCommandBufferToSceneEmulatedRigBuffer(renderContext);
            }
            else
                SetCommandBufferToPlayModeBuffer(renderContext);
        }

        /// <inheritdoc/>
        public override bool IsSupported(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                currentSupportedCamera = camera;
                return true;
            }

            var isSupported = (IsEmulated || EmulatedCompositionLayerUtils.IsRuntimeEmulationSupported())
                && camera == CompositionLayerManager.MainCameraCache;

            currentSupportedCamera = isSupported ? camera : null;
            return isSupported;
        }

        protected override string GetShaderLayerTypeKeyword()
        {
            return k_ShaderLayerTypeKeyword;
        }

        protected override void UpdateMesh(ref Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = GeneratePlaneMesh(1.0f);
            }
        }

        void SetCommandBufferToSceneEmulatedRigBuffer(RenderContext renderContext)
        {
#if UNITY_EDITOR && UNITY_RENDER_PIPELINES_UNIVERSAL
            // Don't create a command buffer if we're building a player.
            if (BuildPipeline.isBuildingPlayer)
                return;

            var rig = SceneEmulatedProjectionRig.CreateOrGet(CompositionLayer, currentSupportedCamera);
            if (rig == null)
                return;

            if (renderContext.m_CommandBuffer != null)
                rig.ClearAndAddCommand(renderContext.m_CommandBuffer);

            if (renderContext.m_RasterCommandBuffer != null)
                rig.ClearAndAddCommand(renderContext.m_RasterCommandBuffer);
#endif
        }

        void SetCommandBufferToPlayModeBuffer(RenderContext renderContext)
        {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            if (renderContext.m_CommandBuffer != null)
                base.AddCommands(renderContext.m_CommandBuffer, new CommandArgs { IsSceneView = false });

            if (renderContext.m_RasterCommandBuffer != null)
                base.AddCommands(renderContext.m_RasterCommandBuffer, new CommandArgs { IsSceneView = false });
#endif
        }

    }
}
