using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.XR.CompositionLayers.Emulation.Implementations;
using System.Linq;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.Scripting.LifecycleManagement;

#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers.Emulation
{

#if UNITY_EDITOR
    /// <summary>
    /// This class used to emulate a Projection Rig CompositionLayer inside of all scene view windows.
    /// </summary>
    [ExecuteInEditMode]
    partial class SceneEmulatedProjectionRig : MonoBehaviour
    {
        internal int callbackOrder => 0;

        [NoAutoStaticsCleanup]
        static Dictionary<Camera, Dictionary<int, SceneEmulatedProjectionRig>> s_CameraToRigs = new();

        Camera m_SceneViewCamera;
        SceneViewProjector m_SceneViewProjector;
        CompositionLayer m_CompositionLayer;
        int m_CompositionLayerId;

        /// <summary>
        /// If no rig currently exists in the cache then this creates a gameObject and adds the Camera + SceneEmulatedProjectionRig components to it.
        /// Newly created rigs are then added to the cache.
        /// </summary>
        /// <param name="compositionLayer">The composition layer that will be emulated.</param>
        /// <param name="sceneViewCamera">The scene camera to send projections to.</param>
        /// <returns>Reference to the SceneEmulatedProjectionRig component.</returns>
        internal static SceneEmulatedProjectionRig CreateOrGet(CompositionLayer compositionLayer, Camera sceneViewCamera)
        {
            if (!CompositionLayerManager.TryGetLayerId(compositionLayer, out int compositionLayerId))
            {
                Debug.LogError("Failed to get layer id for projection rig scene view emulation.");
                return null;
            }

            if (!s_CameraToRigs.ContainsKey(sceneViewCamera))
                s_CameraToRigs.Add(sceneViewCamera, new Dictionary<int, SceneEmulatedProjectionRig>());

            if (s_CameraToRigs[sceneViewCamera].ContainsKey(compositionLayerId))
            {
                // Update the Culling Mask of the camera to match the layer of it's target Composition Layer.
                // Used mainly when a new Projection Layer is created and the target layer is updated.
                var currentRig = s_CameraToRigs[sceneViewCamera][compositionLayerId];
                var currentRigCamera = currentRig.m_SceneViewProjector.SourceCamera;

                var mask = 1 << compositionLayer.gameObject.layer;
                if (currentRigCamera != null && currentRigCamera.cullingMask != mask)
                    currentRigCamera.cullingMask = mask;

                return s_CameraToRigs[sceneViewCamera][compositionLayerId];
            }

            var name = sceneViewCamera.name + "_" + compositionLayerId;
            var gameObject = new GameObject(name);
            gameObject.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

            var rig = gameObject.AddComponent<SceneEmulatedProjectionRig>();
            rig.m_SceneViewCamera = sceneViewCamera;
            rig.m_CompositionLayerId = compositionLayerId;
            rig.m_CompositionLayer = compositionLayer;

            var rigCamera = CreateRigCamera(gameObject, LayerMask.LayerToName(compositionLayer.gameObject.layer));
            rig.m_SceneViewProjector = new SceneViewProjector(rigCamera, sceneViewCamera);

            s_CameraToRigs[sceneViewCamera].Add(compositionLayerId, rig);

            return rig;
        }

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        internal void ClearAndAddCommand(RasterCommandBuffer rcb) => m_SceneViewProjector?.ClearAndAddCommand(rcb);
        internal void ClearAndAddCommand(CommandBuffer cb) => m_SceneViewProjector?.ClearAndAddCommand(cb);

#endif

        internal static void DestroyAllEmulatedRigsForLayerId(int layerId)
        {
            for (var i = 0; i < s_CameraToRigs.Keys.Count; ++i)
            {
                var idToRigs = s_CameraToRigs.ElementAt(i).Value;
                if (idToRigs.TryGetValue(layerId, out var rig))
                    DestroyImmediate(rig.gameObject);
            }
        }

        void Update()
        {
            // If the player is being built, don't update the SceneViewProjector.
            if (BuildPipeline.isBuildingPlayer)
                return;

            // LayerData should be disposed when it's Composition Layer goes out of scope.
            // However, currently it does not call Dispose() during an undo action, so this object must track the CompositionLayer reference.
            if (m_CompositionLayer == null || m_SceneViewCamera == null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            m_SceneViewProjector?.UpdateResources();
        }

        static Camera CreateRigCamera(GameObject gameObject, string layerToRender)
        {
            var rigCamera = gameObject.AddComponent<Camera>();
#if !UNITY_RENDER_PIPELINES_UNIVERSAL
            rigCamera.stereoTargetEye = StereoTargetEyeMask.None;
#endif
            rigCamera.cullingMask = 1 << LayerMask.NameToLayer(layerToRender);
            rigCamera.clearFlags = CameraClearFlags.SolidColor;
            rigCamera.backgroundColor = new Color(0, 0, 0, 0);

            return rigCamera;
        }

        void RemoveRigFromSceneCamera(Camera camera)
        {
            if (s_CameraToRigs.ContainsKey(camera))
            {
                if (s_CameraToRigs[camera].ContainsKey(m_CompositionLayerId))
                    s_CameraToRigs[camera].Remove(m_CompositionLayerId);
            }

            if (s_CameraToRigs.Count == 0)
                s_CameraToRigs.Remove(camera);
        }

        /// <summary>
        /// Subscribe to the scene closing event to cleanup when a scene is closed.
        /// </summary>
        void OnEnable()
        {
            EditorSceneManager.sceneClosing += OnSceneClosing;
        }

        /// <summary>
        /// Cleanup for when this object is destroyed.
        /// There are three ways this object may be destroyed:
        ///     m_SceneViewCamera goes out of scope.
        ///     m_CompositionLayer goes out of scope.
        ///     ProjectionRigEmulationLayerData calls DestroyAllEmulatedRigsForLayerId() when disposed.
        /// </summary>
        void OnDestroy()
        {
            m_CompositionLayer = null;
            RemoveRigFromSceneCamera(m_SceneViewCamera);
        }

        /// <summary>
        /// Cleanup for when this object is destroyed.
        /// There are three ways this object may be destroyed:
        ///     m_SceneViewCamera goes out of scope.
        ///     m_CompositionLayer goes out of scope.
        ///     ProjectionRigEmulationLayerData calls DestroyAllEmulatedRigsForLayerId() when disposed.
        /// </summary>
        void OnDisable()
        {
            EditorSceneManager.sceneClosing -= OnSceneClosing;

            m_SceneViewProjector?.Dispose();
        }

        static void OnSceneClosing(Scene scene, bool removingScene)
        {
            DisposeAllSceneViewProjectors();
        }

        /// <summary>
        /// Disposes of all SceneViewProjectors
        /// </summary>
        static void DisposeAllSceneViewProjectors()
        {
            foreach (var cameraToRig in s_CameraToRigs)
            {
                foreach (var rig in cameraToRig.Value)
                {
                    rig.Value.m_SceneViewProjector.Dispose();
                }
            }
        }

        /// <summary>
        /// This class inherits from ProjectionEmulator and syncs the source camera with the provided scene view camera.
        /// </summary>
        class SceneViewProjector : ProjectionEmulator
        {
            internal SceneViewProjector(Camera sourceCamera, Camera sceneViewCamera) : base(sourceCamera, sceneViewCamera)
            {
                SceneView.duringSceneGui += OnDuringSceneGui;

                // During the first frame of play mode, the scene camera is set to the origin.
                // Disable the camera for that first frame.
                m_SourceCamera.enabled = false;

                // Re-enable the camera after the first frame.
                EditorApplication.delayCall += () =>
                {
                    if (m_SourceCamera != null)
                        m_SourceCamera.enabled = true;
                };
            }

            ~SceneViewProjector() => SceneView.duringSceneGui -= OnDuringSceneGui;

            void SyncWithSceneCamera()
            {
                if (m_SourceCamera.orthographic != m_DestinationCamera.orthographic)
                    m_SourceCamera.orthographic = m_DestinationCamera.orthographic;

                m_SourceCamera.transform.position = m_DestinationCamera.transform.position;
                m_SourceCamera.transform.rotation = m_DestinationCamera.transform.rotation;
                m_SourceCamera.orthographicSize = m_DestinationCamera.orthographicSize;
                m_SourceCamera.fieldOfView = m_DestinationCamera.fieldOfView;
            }

            void OnDuringSceneGui(SceneView sceneView)
            {
                if (m_SourceCamera == null || m_DestinationCamera == null)
                {
                    Dispose();
                    SceneView.duringSceneGui -= OnDuringSceneGui;
                    return;
                }

                if (sceneView.camera == m_DestinationCamera)
                    SyncWithSceneCamera();
            }
        }

        /// <summary>
        /// This class used for emulating projection, but doesn't actually add the command buffer to the destination camera (depends on the EmulatedLayerProvider to add the command buffer).
        /// </summary>
        class ProjectionEmulator : IDisposable
        {
            const float k_ProjectionMeshScale = 1.0f;
            const int k_DepthBufferBits = 24;

            internal Camera SourceCamera => m_SourceCamera;

            protected Camera m_SourceCamera;
            protected Camera m_DestinationCamera;
            protected RenderTexture m_SourceRenderTexture;
            protected Material m_SourceRenderMaterial;
            protected Mesh m_SourceProjectionMesh;
            bool m_Disposed;

            internal ProjectionEmulator(Camera sourceCamera, Camera destinationCamera, int nnum = 0)
            {
                m_DestinationCamera = destinationCamera;
                m_SourceCamera = sourceCamera;
                CreateMaterial();
                UpdateRenderTextureScale();
                CreateProjectionMesh();
            }

            internal virtual void UpdateResources()
            {
                if (m_SourceCamera == null || m_DestinationCamera == null)
                    return;

                UpdateRenderTextureScale();
            }

#if UNITY_RENDER_PIPELINES_UNIVERSAL
            internal void ClearAndAddCommand(RasterCommandBuffer rcb)
            {
                rcb?.DrawMesh(m_SourceProjectionMesh, Matrix4x4.identity, m_SourceRenderMaterial);
            }

            internal void ClearAndAddCommand(CommandBuffer cb)
            {
                cb?.Clear();
                cb?.DrawMesh(m_SourceProjectionMesh, Matrix4x4.identity, m_SourceRenderMaterial);
            }
#endif

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                if (m_SourceCamera != null)
                {
                    m_SourceCamera.targetTexture = null;
                    if (m_SourceCamera.gameObject.activeInHierarchy)
                        CompositionLayerHelper.DestroyCamera(m_SourceCamera);
                }

                if (m_SourceProjectionMesh != null)
                    m_SourceProjectionMesh.Clear();

                if (m_SourceRenderTexture != null)
                {
                    m_SourceRenderTexture.Release();
                    DestroyImmediate(m_SourceRenderTexture);
                }

                DestroyImmediate(m_SourceRenderMaterial);

                m_Disposed = true;
            }

            protected void UpdateRenderTextureScale()
            {
                if (m_SourceRenderTexture?.width == m_DestinationCamera.pixelWidth &&
                    m_SourceRenderTexture?.height == m_DestinationCamera.pixelHeight)
                    return;

                if (m_SourceRenderTexture != null)
                {
                    m_SourceCamera.targetTexture = null;
                    m_SourceRenderTexture.Release();
                    DestroyImmediate(m_SourceRenderTexture);
                }

                m_SourceRenderTexture = new RenderTexture(
                    m_DestinationCamera.pixelWidth,
                    m_DestinationCamera.pixelHeight,
                    k_DepthBufferBits) { name = "SceneProjection" + "_source" };

                m_SourceRenderTexture.Create();
                m_SourceCamera.targetTexture = m_SourceRenderTexture;
                m_SourceRenderMaterial.SetTexture(EmulatedLayerData.k_MainTex, m_SourceCamera.targetTexture);
            }

            void CreateMaterial()
            {
                Shader emulationShader = Shader.Find(CompositionLayerConstants.UberShaderName);
                m_SourceRenderMaterial = new Material(emulationShader);
                m_SourceRenderMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m_SourceRenderMaterial.EnableKeyword(ProjectionRigEmulationLayerData.k_ShaderLayerTypeKeyword);
                m_SourceRenderMaterial.EnableKeyword(EmulatedLayerData.k_SourceTextureMaterialKeyword);
            }

            void CreateProjectionMesh()
            {
                m_SourceProjectionMesh = EmulatedMeshLayerData.GeneratePlaneMesh(k_ProjectionMeshScale);
            }

        }
    }
#endif
}
