using System;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using Unity.Profiling;
using Unity.XR.CompositionLayers.Rendering;
using Unity.XR.CompositionLayers.Emulation.Implementations;
using Unity.Scripting.LifecycleManagement;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers.Emulation
{
    class EmulatedLayerProvider : ILayerProvider
    {
        static readonly ProfilerMarker s_EmulatedLayerProviderCreate = new("EmulatedLayerProvider.Create");
        static readonly ProfilerMarker s_EmulatedLayerProviderRemove = new("EmulatedLayerProvider.Remove");
        static readonly ProfilerMarker s_EmulatedLayerProviderModify = new("EmulatedLayerProvider.Modify");
        static readonly ProfilerMarker s_EmulatedLayerProviderActive = new("EmulatedLayerProvider.Active");
        static readonly ProfilerMarker s_EmulatedLayerProviderSetupRenderPipeline = new("EmulatedLayerProvider.SetupRenderPipeline");

        [NoAutoStaticsCleanup]
        static EmulatedLayerProvider s_Instance;
        [NoAutoStaticsCleanup]
        static bool s_WarnUnsupportedEmulation;
        [NoAutoStaticsCleanup]
        internal static bool s_WarnUnsupportedRenderPipeline;

        Dictionary<int, EmulatedCompositionLayer> m_AllCompositionLayers = new();

        List<EmulatedCompositionLayer> m_SortedLayers = new();

        List<Camera> m_ActiveCameras = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        internal static void ConnectEmulatedLayerProvider()
        {
            CompositionLayerManager.ManagerStarted += OnCompositionLayerManagerStarted;
            CompositionLayerManager.ManagerStopped += OnCompositionLayerManagerStopped;
            s_WarnUnsupportedEmulation = true;
            s_WarnUnsupportedRenderPipeline = true;

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

#if UNITY_RENDER_PIPELINES_UNIVERSAL
            EmulationLayerUniversalScriptableRendererPass.RegisterScriptableRendererPass();
            EmulationColorScaleBiasPass.RegisterScriptableRendererPass();
#endif
#if UNITY_RENDER_PIPELINES_HDRENDER
#if UNITY_EDITOR
#pragma warning disable CS0618
            if (RenderPipelineUtility.IsHDRenderPipeline())
            {
                Debug.LogWarning("High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.");
            }
#pragma warning restore CS0618
#endif
            EmulationLayerHighDefinitionVolumeManager.ActivateCustomPassVolumes();
#endif
        }

        static void OnCompositionLayerManagerStopped()
        {
            if (s_Instance == null)
                return;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
            EmulationLayerUniversalScriptableRendererPass.UnregisterScriptableRendererPass();
            EmulationColorScaleBiasPass.UnregisterScriptableRendererPass();
#endif
#if UNITY_RENDER_PIPELINES_HDRENDER
            EmulationLayerHighDefinitionVolumeManager.DeactivateCustomPassVolumes();
#endif

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
            TearDownRenderPipelineCommandBuffers();

            foreach (var compositionLayer in m_AllCompositionLayers)
            {
                compositionLayer.Value?.Dispose();
            }

            m_AllCompositionLayers.Clear();
        }

        public void UpdateLayers(List<CompositionLayerManager.LayerInfo> createdLayers, List<int> removedLayers,
            List<CompositionLayerManager.LayerInfo> modifiedLayers, List<CompositionLayerManager.LayerInfo> activeLayers)
        {
            TearDownRenderPipelineCommandBuffers();

            AddCreatedLayers(createdLayers);
            RemoveDestroyedLayers(removedLayers);
            ModifyChangedLayers(modifiedLayers);
            UpdateActiveStateOnLayers(activeLayers);

            SetupRenderPipelineCommandBuffers();
        }

        void ModifyChangedLayers(List<CompositionLayerManager.LayerInfo> modifiedLayers)
        {
            if (modifiedLayers.Count == 0)
                return;

            foreach (var layerInfo in modifiedLayers)
            {
                s_EmulatedLayerProviderModify.Begin();

                if (!m_AllCompositionLayers.TryGetValue(layerInfo.Id, out var emulatedLayer))
                    emulatedLayer = CreateEmulationLayerObject(layerInfo);

                emulatedLayer?.ModifyLayer();

                s_EmulatedLayerProviderModify.End();
            }
        }

        void UpdateActiveStateOnLayers(List<CompositionLayerManager.LayerInfo> activeLayers)
        {
            if (activeLayers.Count == 0)
                return;


            foreach (var layerInfo in activeLayers)
            {
                s_EmulatedLayerProviderActive.Begin();

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
                    {
                        s_EmulatedLayerProviderActive.End();
                        return;
                    }

                    if (emulatedLayer.EmulatedLayerData == null)
                        emulatedLayer.ModifyLayer();

                    emulatedLayer.UpdateLayer();
                }

                s_EmulatedLayerProviderActive.End();
            }
        }

        void RemoveDestroyedLayers(List<int> removedLayers)
        {
            if (removedLayers.Count == 0)
                return;

            foreach (var layerId in removedLayers)
            {
                s_EmulatedLayerProviderRemove.Begin();

                if (m_AllCompositionLayers.TryGetValue(layerId, out var emulatedCompositionLayer))
                    emulatedCompositionLayer?.Dispose();

                m_AllCompositionLayers.Remove(layerId);

                s_EmulatedLayerProviderRemove.End();
            }
        }

        void AddCreatedLayers(List<CompositionLayerManager.LayerInfo> createdLayers)
        {
            if (createdLayers.Count == 0)
                return;

            foreach (var layerInfo in createdLayers)
            {
                s_EmulatedLayerProviderCreate.Begin();

                if (!m_AllCompositionLayers.ContainsKey(layerInfo.Id))
                    CreateEmulationLayerObject(layerInfo);

                s_EmulatedLayerProviderCreate.End();
            }
        }

        public void LateUpdate() { }

        /// <summary>
        /// Whether emulated layers may be drawn into <paramref name="camera"/>.
        /// </summary>
        internal static bool IsSupported(Camera camera)
        {
            if(s_Instance == null)
                return false;

            return s_Instance.m_ActiveCameras.Contains(camera);
        }

        void SetupRenderPipelineCommandBuffers()
        {
            if (!CompositionLayerManager.ManagerActive)
                return;

            s_EmulatedLayerProviderSetupRenderPipeline.Begin();

            UpdateActiveCamerasAndSortedLayers();

            var isSupported = false;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            isSupported |= RenderPipelineUtility.IsUniversalRenderPipeline();
#endif
#if UNITY_RENDER_PIPELINES_HDRENDER
#pragma warning disable CS0618
            isSupported |= RenderPipelineUtility.IsHDRenderPipeline();
#pragma warning restore CS0618
#endif
            if (isSupported)
            {
#if UNITY_RENDER_PIPELINES_UNIVERSAL || UNITY_RENDER_PIPELINES_HDRENDER
                foreach (var commandBufferLayer in m_SortedLayers)
                {
                    EmulationLayerScriptableRendererManager.Add(commandBufferLayer.EmulatedLayerData, commandBufferLayer.Order);
                }
#endif
            }
            else
            {
                WarnUnsupportedRenderPipeline();
            }

            s_EmulatedLayerProviderSetupRenderPipeline.End();
        }

        void TearDownRenderPipelineCommandBuffers()
        {
#if UNITY_RENDER_PIPELINES_HDRENDER || UNITY_RENDER_PIPELINES_UNIVERSAL
            EmulationLayerScriptableRendererManager.Clear();
#endif
        }

        void WarnUnsupportedRenderPipeline()
        {
            if (!s_WarnUnsupportedRenderPipeline || m_SortedLayers.Count == 0)
            {
                return;
            }

            s_WarnUnsupportedRenderPipeline = false;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            Debug.LogWarning("Composition layer emulation requires the Universal Render Pipeline. Layers are not emulated in this project.");
#endif
        }

        void AddEmulationToActiveCamera()
        {
            var mainCamera = CompositionLayerManager.MainCameraCache;
            if (mainCamera != null)
                m_ActiveCameras.Add(mainCamera);
        }

        /// <summary>
        /// Whether any layer is on the <see cref="CompositionLayerRenderPath.Emulated"/> path, meaning emulation is
        /// that layer's only rendering path and the main camera has to draw it.
        /// </summary>
        bool HasEmulatedLayers()
        {
            foreach (var compositionLayerSet in m_AllCompositionLayers)
            {
                var compositionLayer = compositionLayerSet.Value?.CompositionLayer;
                if (compositionLayer != null && compositionLayer.RenderPath == CompositionLayerRenderPath.Emulated)
                    return true;
            }

            return false;
        }

        void UpdateActiveCamerasAndSortedLayers()
        {
            if (!CompositionLayerManager.ManagerActive)
                return;

            m_ActiveCameras.Clear();

            if (!Application.isPlaying ||
                (HasEmulatedLayers() && EmulatedCompositionLayerUtils.IsEmulationAllowedInPlayer()))
                AddEmulationToActiveCamera();

#if UNITY_EDITOR
            foreach (var sceneViewObject in SceneView.sceneViews)
            {
                if (sceneViewObject is SceneView sceneView)
                    m_ActiveCameras.Add(sceneView.camera);
            }
#endif
            m_SortedLayers.Clear();

#if UNITY_EDITOR
            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            bool isUsingPrefabStage = currentPrefabStage != null && currentPrefabStage.scene.IsValid();
#endif
            // Gather command buffer based layers
            foreach (var compositionLayerSet in m_AllCompositionLayers)
            {
                if (compositionLayerSet.Value == null || !compositionLayerSet.Value.Enabled)
                    continue;

#if UNITY_EDITOR
                var compLayerPrefabStage = PrefabStageUtility.GetPrefabStage(compositionLayerSet.Value.CompositionLayer.gameObject);
                if (isUsingPrefabStage && currentPrefabStage != compLayerPrefabStage)
                    continue;
#endif

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
