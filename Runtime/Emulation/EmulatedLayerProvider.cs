using System;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.XR.CompositionLayers.Emulation
{
    class EmulatedLayerProvider : ILayerProvider
    {
        static readonly CameraEvent[] k_DefaultUnderlayCameraEvents = { CameraEvent.BeforeForwardOpaque };
        static readonly CameraEvent[] k_DefaultOverlayCameraEvents = { CameraEvent.AfterImageEffects };
        static readonly CameraEvent[] k_DeferredUnderlayCameraEvents = { CameraEvent.BeforeGBuffer };
        static readonly CameraEvent[] k_DeferredOverlayCameraEvents = { CameraEvent.AfterImageEffects };

        static EmulatedLayerProvider s_Instance; // Note: EmulatedLayerProvider is singleton.
        static bool s_WarnUnsupportedEmulation;

        Dictionary<int, EmulatedCompositionLayer> m_AllCompositionLayers = new();

        List<EmulatedCompositionLayer> m_SortedLayers = new();

        HashSet<EmulatedCameraData> m_ActiveCameras = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        internal static void ConnectEmulatedLayerProvider()
        {
            CompositionLayerManager.ManagerStarted += OnCompositionLayerManagerStarted;
            CompositionLayerManager.ManagerStopped += OnCompositionLayerManagerStopped;
            s_WarnUnsupportedEmulation = true;

            if (CompositionLayerManager.ManagerActive)
                OnCompositionLayerManagerStarted();
        }

        internal static void DisconnectEmulatedLayerProvider()
        {
            CompositionLayerManager.ManagerStarted -= OnCompositionLayerManagerStarted;
            CompositionLayerManager.ManagerStopped -= OnCompositionLayerManagerStopped;

            if (s_Instance != null)
                OnCompositionLayerManagerStopped();
        }

        static void OnCompositionLayerManagerStarted()
        {
            if (s_Instance == null)
                s_Instance = new EmulatedLayerProvider();

            if (!CompositionLayerManager.ManagerActive)
                return;

            CompositionLayerManager.Instance.EmulationLayerProvider = s_Instance;
        }

        static void OnCompositionLayerManagerStopped()
        {
            if (s_Instance == null)
                return;

            s_Instance.CleanupState();

            if (!CompositionLayerManager.ManagerActive)
                return;

            if (CompositionLayerManager.Instance.EmulationLayerProvider == s_Instance)
                CompositionLayerManager.Instance.EmulationLayerProvider = null;
        }

        EmulatedCompositionLayer CreateEmulationLayerObject(CompositionLayerManager.LayerInfo layerInfo)
        {
            var layer = layerInfo.Layer;
            if (layer == null)
                return null;

            var emulatedLayer = new EmulatedCompositionLayer();
            emulatedLayer.CompositionLayer = layer;

            if (layer.LayerData != null)
            {
                var emulatedLayerDataType = EmulatedCompositionLayerUtils.GetEmulatedLayerDataType(layer.LayerData.GetType());

                if (emulatedLayerDataType != null)
                    emulatedLayer.ChangeEmulatedLayerDataType(emulatedLayerDataType);
            }

            emulatedLayer.ModifyLayer();
            emulatedLayer.UpdateLayer();

            if (!m_AllCompositionLayers.ContainsKey(layerInfo.Id))
                m_AllCompositionLayers.Add(layerInfo.Id, emulatedLayer);
            else
                m_AllCompositionLayers[layerInfo.Id] = emulatedLayer;

            return emulatedLayer;
        }

        public void SetInitialState(List<CompositionLayerManager.LayerInfo> layers)
        {
            m_AllCompositionLayers.Clear();
            AddCreatedLayers(layers);
        }

        public void CleanupState()
        {
            var usingLegacy = GraphicsSettings.currentRenderPipeline == null;

            if (usingLegacy)
                TearDownLegacyCommandBuffers();

            foreach (var compositionLayer in m_AllCompositionLayers)
            {
                compositionLayer.Value?.Dispose();
            }

            m_AllCompositionLayers.Clear();
        }

        public void UpdateLayers(List<CompositionLayerManager.LayerInfo> createdLayers, List<int> removedLayers,
            List<CompositionLayerManager.LayerInfo> modifiedLayers, List<CompositionLayerManager.LayerInfo> activeLayers)
        {
            var usingLegacy = GraphicsSettings.currentRenderPipeline == null;

            if (usingLegacy)
                TearDownLegacyCommandBuffers();
            else
                TearDownRenderPipelineCommandBuffers();

            AddCreatedLayers(createdLayers);
            RemoveDestroyedLayers(removedLayers);
            ModifyChangedLayers(modifiedLayers);
            UpdateActiveStateOnLayers(activeLayers);

            if (usingLegacy)
                SetupLegacyCommandBuffers();
            else
                SetupRenderPipelineCommandBuffers();
        }

        void ModifyChangedLayers(List<CompositionLayerManager.LayerInfo> modifiedLayers)
        {
            if (modifiedLayers.Count == 0)
                return;

            foreach (var layerInfo in modifiedLayers)
            {
                if (!m_AllCompositionLayers.TryGetValue(layerInfo.Id, out var emulatedLayer))
                    emulatedLayer = CreateEmulationLayerObject(layerInfo);

                emulatedLayer?.ModifyLayer();
            }
        }

        void UpdateActiveStateOnLayers(List<CompositionLayerManager.LayerInfo> activeLayers)
        {
            if (activeLayers.Count == 0)
                return;

            foreach (var layerInfo in activeLayers)
            {
                if (!m_AllCompositionLayers.TryGetValue(layerInfo.Id, out var emulatedLayer))
                {
                    emulatedLayer = CreateEmulationLayerObject(layerInfo);
                    emulatedLayer?.ModifyLayer();
                }

                if (emulatedLayer != null)
                {
                    // Undo/Redo can cause the CompositionLayer reference to be lost
                    if (emulatedLayer.CompositionLayer == null)
                    {
                        emulatedLayer.Dispose();
                        emulatedLayer = CreateEmulationLayerObject(layerInfo);
                        emulatedLayer?.ModifyLayer();
                    }

                    if (emulatedLayer == null)
                        return;

                    if (emulatedLayer.EmulatedLayerData == null)
                        emulatedLayer.ModifyLayer();


                    emulatedLayer.UpdateLayer();
                }
            }
        }

        void RemoveDestroyedLayers(List<int> removedLayers)
        {
            if (removedLayers.Count == 0)
                return;

            foreach (var layerId in removedLayers)
            {
                if (m_AllCompositionLayers.TryGetValue(layerId, out var emulatedCompositionLayer))
                    emulatedCompositionLayer?.Dispose();
                m_AllCompositionLayers.Remove(layerId);
            }
        }

        void AddCreatedLayers(List<CompositionLayerManager.LayerInfo> createdLayers)
        {
            if (createdLayers.Count == 0)
                return;

            foreach (var layerInfo in createdLayers)
            {
                if (!m_AllCompositionLayers.ContainsKey(layerInfo.Id))
                    CreateEmulationLayerObject(layerInfo);
            }
        }

        public void LateUpdate() { }

        internal static CameraEvent[] GetUnderlayCameraEvents(Camera camera)
        {
            return camera.actualRenderingPath != RenderingPath.DeferredShading ?
                k_DefaultUnderlayCameraEvents : k_DeferredUnderlayCameraEvents;
        }

        internal static CameraEvent[] GetOverlayCameraEvents(Camera camera)
        {
            return camera.actualRenderingPath != RenderingPath.DeferredShading ?
                k_DefaultOverlayCameraEvents : k_DeferredOverlayCameraEvents;
        }

        void SetupRenderPipelineCommandBuffers()
        {
#if UNITY_RENDER_PIPELINES_UNIVERSAL || UNITY_RENDER_PIPELINES_HDRENDER
            if (!CompositionLayerManager.ManagerActive)
                return;

            UpdateActiveCamerasAndSortedLayers();

#if UNITY_RENDER_PIPELINES_HDRENDER
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            bool isHDRP = GraphicsSettings.currentRenderPipeline.name.StartsWith("HDRP"); // Selectable. (Both packages are installed.)
#else // UNITY_RENDER_PIPELINES_UNIVERSAL
            bool isHDRP = true; // Always enabled. HDRP package is installed & URP package isn't installed.
#endif // UNITY_RENDER_PIPELINES_UNIVERSAL
#else // UNITY_RENDER_PIPELINES_HDRENDER
            bool isHDRP = false; // Unsupported. HDRP package isn't installed.
#endif // UNITY_RENDER_PIPELINES_HDRENDER
            bool isURP = !isHDRP;

#if UNITY_RENDER_PIPELINES_HDRENDER
            if (isHDRP)
            {
                EmulationLayerHighDefinitionCustomPassManager.Bind(m_ActiveCameras);
                foreach (var commandBufferLayer in m_SortedLayers)
                {
                    EmulationLayerHighDefinitionCustomPassManager.Add(commandBufferLayer.EmulatedLayerData, commandBufferLayer.Order);
                }
            }
#endif // UNITY_RENDER_PIPELINES_HDRENDER
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            if (isURP)
            {
                foreach (var commandBufferLayer in m_SortedLayers)
                {
                    foreach (var cameraData in m_ActiveCameras)
                    {
                        if (commandBufferLayer.EmulatedLayerData.IsSupported(cameraData.Camera))
                        {
                            var commandArgs = new EmulatedLayerData.CommandArgs(cameraData);
                            var commandBuffer = commandBufferLayer.EmulatedLayerData.UpdateCommandBuffer(commandArgs);
                            EmulationLayerUniversalScriptableRendererManager.AddCommandBuffer(cameraData.Camera, commandBuffer, commandBufferLayer.Order);
                        }
                    }
                }
            }
#endif // UNITY_RENDER_PIPELINES_UNIVERSAL
#endif // UNITY_RENDER_PIPELINES_UNIVERSAL || UNITY_RENDER_PIPELINES_HDRENDER
        }

        void TearDownRenderPipelineCommandBuffers()
        {
#if UNITY_RENDER_PIPELINES_HDRENDER
            EmulationLayerHighDefinitionCustomPassManager.Clear();
#endif

#if UNITY_RENDER_PIPELINES_UNIVERSAL
            EmulationLayerUniversalScriptableRendererManager.Clear();
#endif
        }

        void SetupLegacyCommandBuffers()
        {
            if (!CompositionLayerManager.ManagerActive)
                return;

            UpdateActiveCamerasAndSortedLayers();

            AddCommandBuffers();
        }

        void UpdateActiveCamerasAndSortedLayers()
        {
            if (!CompositionLayerManager.ManagerActive)
                return;

            m_ActiveCameras.Clear();
            if (Application.isPlaying)
            {
                var mainCamera = Camera.main;
                if (mainCamera)
                {
                    if (EmulatedCompositionLayerUtils.EmulationInPlayMode || EmulatedCompositionLayerUtils.EmulationInStandalone)
                        m_ActiveCameras.Add(new EmulatedCameraData(mainCamera));
                }
            }

#if UNITY_EDITOR
            foreach (var sceneViewObject in SceneView.sceneViews)
            {
                if (sceneViewObject is SceneView sceneView)
                    m_ActiveCameras.Add(new EmulatedCameraData(sceneView.camera));
            }
#endif

            m_SortedLayers.Clear();

            // Gather command buffer based layers
            foreach (var compositionLayerSet in m_AllCompositionLayers)
            {
                if (compositionLayerSet.Value == null || !compositionLayerSet.Value.Enabled)
                    continue;

                var emulatedRenderLayerData = compositionLayerSet.Value.EmulatedLayerData;
                if (emulatedRenderLayerData != null)
                {
                    if (!emulatedRenderLayerData.IsInitialized)
                        continue;

                    m_SortedLayers.Add(compositionLayerSet.Value);
                }
            }

            if (m_SortedLayers.Count == 0)
                return;

            // Sort emulated render layers
            m_SortedLayers.Sort(EmulatedLayerDataSorter);
        }

        void AddCommandBuffers()
        {
            foreach (var commandBufferLayer in m_SortedLayers)
            {
                commandBufferLayer.AddCommandBuffer(m_ActiveCameras);
            }
        }

        void TearDownLegacyCommandBuffers()
        {
            if (m_SortedLayers.Count == 0)
                return;

            foreach (var commandBufferLayer in m_SortedLayers)
            {
                commandBufferLayer.RemoveCommandBuffer(m_ActiveCameras);
            }
        }

        static int EmulatedLayerDataSorter(EmulatedCompositionLayer lhs, EmulatedCompositionLayer rhs)
        {
            return lhs.Order.CompareTo(rhs.Order);
        }

        internal static void WarnUnsupportedEmulation(Camera camera, CompositionLayer layer)
        {
            if (s_WarnUnsupportedEmulation)
            {
                Debug.LogWarning($"Emulation of composition layers in Game View is not supported when XR device is connected and active.");
                s_WarnUnsupportedEmulation = false;
            }
        }
    }
}
