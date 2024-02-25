using System;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEditor;
using Unity.XR.CoreUtils;

namespace Unity.XR.CompositionLayers.Emulation.Implementations
{
    /// <summary>
    /// Base for emulating <see cref="ProjectionLayerRigData"/>. Used to emulate a full screen texture rendering to the compositor.
    /// </summary>
    [EmulatedLayerDataType(typeof(ProjectionLayerRigData))]
    internal class ProjectionRigEmulationLayerData : EmulatedMeshLayerData
    {
        public const string k_ShaderLayerTypeKeyword = "COMPOSITION_LAYERTYPE_PROJECTION";

        public const int k_LayerIndexAfterDefaults = 7;

        // Caches the last supported camera so this object knows which camera it's preparing command buffers for.
        Camera currentSupportedCamera;
        Camera mainCameraCache;
        // Caches cameras and textures for left and right eyes
        Camera leftCam;
        Camera rightCam;
        RenderTexture m_emulationLeftEyeTexture;
        RenderTexture m_emulationRightEyeTexture;

        bool exitingPlayMode;

        // Caches a command buffer to use during play mode.
        CommandBuffer m_playModeCommandBuffer = new CommandBuffer();

        int m_cachedLayer = -1;

        public ProjectionRigEmulationLayerData()
        {
            if (Camera.main == null)
                return;
            mainCameraCache = Camera.main;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        protected internal override void UpdateEmulatedLayerData()
        {
            base.UpdateEmulatedLayerData();

            if (mainCameraCache == null)
                return;
            //re-create render texture if detected gameview window size changed
            if (m_emulationLeftEyeTexture == null || m_emulationRightEyeTexture == null || m_emulationLeftEyeTexture.width != mainCameraCache.pixelWidth || m_emulationLeftEyeTexture.height != mainCameraCache.pixelHeight)
            {
                CreateAndSetRenderTexture(mainCameraCache.pixelWidth, mainCameraCache.pixelHeight, 24);
            }

            // Ensure that the projection rig cameras have the same local pose offset as the main camera.
            leftCam?.transform.parent.SetWorldPose(GetTotalLocalPoseOffset(mainCameraCache.transform));
        }

        private Pose GetTotalLocalPoseOffset(Transform currentTransform)
        {
            var totalLocalPoseOffset = Pose.identity;

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

            if (leftCam == null && rightCam == null)
            {
                leftCam = layer.gameObject.transform.GetChild(0).GetComponent<Camera>();
                rightCam = layer.gameObject.transform.GetChild(1).GetComponent<Camera>();
            }
            leftCam.targetTexture = null;
            rightCam.targetTexture = null;
            if (m_emulationLeftEyeTexture != null)
            {
                m_emulationLeftEyeTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_emulationLeftEyeTexture);
            }
            m_emulationLeftEyeTexture = new RenderTexture((int)width, (int)height, 0, RenderTextureFormat.ARGB32) { name = layer.name + "_left"};
            m_emulationLeftEyeTexture.Create();

            if (m_emulationRightEyeTexture != null)
            {
                m_emulationRightEyeTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_emulationRightEyeTexture);
            }

            m_emulationRightEyeTexture = new RenderTexture((int)width, (int)height, 0, RenderTextureFormat.ARGB32) { name = layer.name + "_right" };
            m_emulationRightEyeTexture.Create();
            
            if (layer != null)
            {
                var textExt = layer.GetComponent<TexturesExtension>();
                if (textExt != null)
                {
                    textExt.LeftTexture = m_emulationLeftEyeTexture;
                    textExt.RightTexture = m_emulationRightEyeTexture;
                }
                if (leftCam == null && rightCam == null)
                {
                    leftCam = layer.gameObject.transform.GetChild(0).GetComponent<Camera>();
                    rightCam = layer.gameObject.transform.GetChild(1).GetComponent<Camera>();
                }
                leftCam.targetTexture = m_emulationLeftEyeTexture;
                rightCam.targetTexture = m_emulationRightEyeTexture;
            }
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            exitingPlayMode = state == PlayModeStateChange.ExitingPlayMode;
        }
#endif
        private void DestroyRig()
        {
#if UNITY_EDITOR
            // Don't destroy the layer inside of play mode
            // as all changes will be reverted on unplay
            if(exitingPlayMode) return;

            if (CompositionLayer == null) return;

            var layer = CompositionLayer;
            var textExt = layer.GetComponent<TexturesExtension>();

            if (layer == null || textExt == null) return;

            // Remove references on the TextureExtension
            if (textExt != null)
            {
                textExt.LeftTexture = null;
                textExt.RightTexture = null;

                // Show the TextureExtension component
                textExt.hideFlags = HideFlags.None;
            }

            // Destroy the cameras
            if (leftCam != null)
            {
                leftCam.targetTexture = null;
                GameObject.DestroyImmediate(leftCam.gameObject);
            }

            if (rightCam != null)
            {
                rightCam.targetTexture = null;
                GameObject.DestroyImmediate(rightCam.gameObject);
            }

            // Release render textures
            if (m_emulationLeftEyeTexture != null)
            {
                m_emulationLeftEyeTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_emulationLeftEyeTexture);
                m_emulationLeftEyeTexture = null;
            }

            if (m_emulationRightEyeTexture != null)
            {
                m_emulationRightEyeTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_emulationRightEyeTexture);
                m_emulationRightEyeTexture = null;
            }

            // Reset to default layer
            Transform.gameObject.layer = 0;
#endif
        }

        /// <summary>
        /// There are three ways this object may be Disposed
        ///     The layer is changed from it's current type.
        ///     The layer is destroyed or deleted.
        ///     Playmode is exited.
        /// </summary>
        public override void Dispose()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DestroyRig();
            SceneEmulatedProjectionRig.DestroyAllEmulatedRigsForLayerId(CompositionLayer.GetInstanceID());
#endif
            base.Dispose();
        }

        private void RemoveLayer(int layer)
        {
#if UNITY_EDITOR
            var layerName = LayerMask.LayerToName(m_cachedLayer);
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty l = layersProp.GetArrayElementAtIndex(i);

                // Check if the layer name exists and it is not a built-in layer
                if (l.stringValue.Equals(layerName) && i > k_LayerIndexAfterDefaults)
                {
                    SerializedProperty newLayerProperty = layersProp.GetArrayElementAtIndex(i);
                    newLayerProperty.stringValue = String.Empty;

                    tagManager.ApplyModifiedProperties();
                }
            }
#endif
        }

        protected override void PrepareCommands()
        {
            if (currentSupportedCamera == null)
                return;

            if (currentSupportedCamera.cameraType == CameraType.SceneView)
            {
                // If IsInvalidatedCommandBuffer is true and the command buffer is already initialized then AddCommands() won't be called.
                // Set the command buffer at this stage so this object sends the correct buffer to the appropriate scene camera.
                if (!m_CommandBufferTempSceneView.IsInvalidated)
                    SetCommandBufferToSceneEmulatedRigBuffer();
            }
            else
            {
                base.PrepareCommands();
                if (!m_CommandBufferTemp.IsInvalidated)
                    SetCommandBufferToPlayModeBuffer();
            }
        }

        protected override void AddCommands(CommandBuffer commandBuffer, CommandArgs commandArgs)
        {
            if (currentSupportedCamera == null)
                return;
            if (currentSupportedCamera.cameraType == CameraType.SceneView)
                SetCommandBufferToSceneEmulatedRigBuffer();
            else
                SetCommandBufferToPlayModeBuffer();
        }

        /// <inheritdoc/>
        public override bool IsSupported(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                currentSupportedCamera = camera;
                return true;
            }

            var isSupported = !Application.isPlaying;
#if ENABLE_UNITY_VR
            isSupported = isSupported || !XRSettings.isDeviceActive;
#endif

            if (isSupported)
                currentSupportedCamera = camera;

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

        void SetCommandBufferToSceneEmulatedRigBuffer()
        {
#if UNITY_EDITOR
            var rig = SceneEmulatedProjectionRig.CreateOrGet(CompositionLayer, currentSupportedCamera);
            rig.ClearAndAddCommand();
            m_CommandBufferTempSceneView = new CommandBufferTemp { CommandBuffer = rig.CommandBufffer, IsInvalidated = false };

            if(CompositionLayer.gameObject.layer > k_LayerIndexAfterDefaults) 
                m_cachedLayer = CompositionLayer.gameObject.layer;
#endif
        }

        void SetCommandBufferToPlayModeBuffer()
        {
            m_playModeCommandBuffer.Clear();
            base.AddCommands(m_playModeCommandBuffer, new CommandArgs { IsSceneView = false });
            m_CommandBufferTemp = new CommandBufferTemp { CommandBuffer = m_playModeCommandBuffer, IsInvalidated = false }; ;
        }
    }
}
