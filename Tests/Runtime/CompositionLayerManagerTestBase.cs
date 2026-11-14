using System;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using LayerInfo = Unity.XR.CompositionLayers.Services.CompositionLayerManager.LayerInfo;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    // Implements ILayerProvider so testing enviorment pretends to be a platform.
    class CompositionLayerManagerTestBase : ILayerProvider
    {
        protected bool hasCreatedLayers;
        protected bool hasRemovedLayers;
        protected bool hasModifiedLayers;
        protected bool hasActiveLayers;

        protected List<LayerInfo> lastCreatedLayers = new();
        protected List<int> lastRemovedLayers = new();
        protected List<LayerInfo> lastModifiedLayers = new();
        protected List<LayerInfo> lastActiveLayers = new();

        // Every layer created via the CreateActiveLayer helpers is tracked here so Teardown can destroy
        // it even if it never became active. The CompositionLayerManager is a process-wide singleton, so
        // any layer left in the scene leaks into the next test's OccupiedLayers/known-layers state.
        protected readonly List<CompositionLayer> m_TrackedLayers = new();

        // The base occupies the native provider slot, so its own reports are the native side. The emulation
        // slot holds a second recorder in place of the real EmulatedLayerProvider.
        protected readonly RecordingLayerProvider m_NativeRecorder = new("native");
        protected readonly RecordingLayerProvider m_EmulationRecorder = new("emulation");

        AwaitableCompletionSource m_UpdateCompletion;
        bool m_UpdateReported;
        PlatformProvider m_ReplacedPlatformProvider;
        Type m_ReplacedPassthroughLayerType;

        [SetUp]
        public void SetUp()
        {
            Setup();
        }

        [TearDown]
        public void TearDown()
        {
            Teardown();
        }

        protected virtual void Setup()
        {
            hasCreatedLayers = false;
            hasRemovedLayers = false;
            hasModifiedLayers = false;
            hasActiveLayers = false;

            lastCreatedLayers.Clear();
            lastRemovedLayers.Clear();
            lastModifiedLayers.Clear();
            lastActiveLayers.Clear();

            m_NativeRecorder.Clear();
            m_EmulationRecorder.Clear();

            m_ReplacedPlatformProvider = PlatformManager.ActivePlatformProvider;
            m_ReplacedPassthroughLayerType = CompositionLayerManager.PassthroughLayerType;
            PlatformManager.ActivePlatformProvider = new TestPlatformProvider();

            // The manager stops itself on any update where no layers exist, and the real EmulatedLayerProvider
            // reclaims the emulation slot from ManagerStarted every time it restarts. Unsubscribing it for the
            // duration of the test is what keeps the recorder installed across those restarts.
            EmulatedLayerProvider.DisconnectEmulatedLayerProvider();

            // Teardown stops the manager, so Instance stays null here until something restarts it.
            CompositionLayerManager.StartCompositionLayerManager();
            CompositionLayerManager.Instance.ClearAllState();
            CompositionLayerManager.Instance.EnsureSceneCompositionManager();

            ResetUpdateSignal();
            CompositionLayerManager.Instance.LayerProvider = this;
            CompositionLayerManager.Instance.EmulationLayerProvider = m_EmulationRecorder;
        }

        protected virtual void Teardown()
        {
            if (CompositionLayerManager.ManagerActive)
            {
                CompositionLayerManager.Instance.LayerProvider = null;
                CompositionLayerManager.Instance.EmulationLayerProvider = null;
            }

            // Setup sets a provider, so without this every later test in the Play session starts with one ever set.
            CompositionLayerManager.ResetLayerProviderHistory();
            PlatformManager.ActivePlatformProvider = m_ReplacedPlatformProvider;
            CompositionLayerManager.PassthroughLayerType = m_ReplacedPassthroughLayerType;

            ResetUpdateSignal();

            foreach (var layer in m_TrackedLayers)
            {
                if (layer)
                    UnityObject.DestroyImmediate(layer.gameObject);
            }
            m_TrackedLayers.Clear();

            // Reset the manager, then immediately destroy every CompositionLayer still in the scene,
            // including the hidden auto-created "Default Scene Layer". In play mode StopCompositionLayerManager
            // destroys that layer with a deferred Object.Destroy, so without this immediate sweep the hidden
            // default layers linger, accumulate into duplicates, and survive play-mode exit (HideAndDontSave).
            // In edit mode a duplicate is then validated and DestroyImmediate'd from within OnEnable, which is
            // illegal and logs "Destroying object ... is not allowed at this time" plus a duplicate-layer warning.
            CompositionLayerManager.StopCompositionLayerManager();
            var sceneLayers = UnityObject.FindObjectsByType<CompositionLayer>(FindObjectsInactive.Include);
            foreach (var layer in sceneLayers)
            {
                if (layer)
                    UnityObject.DestroyImmediate(layer.gameObject);
            }

            lastCreatedLayers.Clear();
            lastRemovedLayers.Clear();
            lastModifiedLayers.Clear();
            lastActiveLayers.Clear();

            hasCreatedLayers = false;
            hasRemovedLayers = false;
            hasModifiedLayers = false;
            hasActiveLayers = false;

            EmulatedLayerProvider.ConnectEmulatedLayerProvider();
        }

        public void SetInitialState(List<LayerInfo> layers) { }

        public void CleanupState() { }

        public void UpdateLayers(List<LayerInfo> createdLayers, List<int> removedLayers,
            List<LayerInfo> modifiedLayers, List<LayerInfo> activeLayers)
        {
            lastCreatedLayers.Clear();
            lastRemovedLayers.Clear();
            lastModifiedLayers.Clear();
            lastActiveLayers.Clear();

            if (createdLayers != null)
            {
                hasCreatedLayers = createdLayers.Count > 0;
                lastCreatedLayers.AddRange(createdLayers);
            }
            else
            {
                hasCreatedLayers = false;
            }

            if (removedLayers != null)
            {
                hasRemovedLayers = removedLayers.Count > 0;
                lastRemovedLayers.AddRange(removedLayers);
            }
            else
            {
                hasRemovedLayers = false;
            }

            if (modifiedLayers != null)
            {
                hasModifiedLayers = modifiedLayers.Count > 0;
                lastModifiedLayers.AddRange(modifiedLayers);
            }
            else
            {
                hasModifiedLayers = false;
            }

            if (activeLayers != null)
            {
                hasActiveLayers = activeLayers.Count > 0;
                lastActiveLayers.AddRange(activeLayers);
            }
            else
            {
                hasActiveLayers = false;
            }

            m_NativeRecorder.UpdateLayers(createdLayers, removedLayers, modifiedLayers, activeLayers);

            m_UpdateReported = true;
        }

        public void LateUpdate()
        {
            if (!m_UpdateReported)
                return;

            // Completing resumes the awaiting test synchronously, and it may request the next cycle
            // before this method returns.
            var completion = m_UpdateCompletion;
            ResetUpdateSignal();

            completion?.TrySetResult();
        }

        public void OnRenderObject() { }

        /// Waits for the manager to run one full update cycle and report it to this provider.
        protected Awaitable NextManagerUpdate()
        {
            m_UpdateReported = false;
            m_UpdateCompletion = new AwaitableCompletionSource();

            return m_UpdateCompletion.Awaitable;
        }

        /// Waits for one update cycle as seen by the emulation slot. Use this while the native provider is detached,
        /// because NextManagerUpdate only completes from the native slot.
        protected async Awaitable NextEmulationUpdate()
        {
            var reportCount = m_EmulationRecorder.Reports.Count;
            while (m_EmulationRecorder.Reports.Count == reportCount)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        /// Clears the native provider slot the way application pause does. Teardown is unaffected.
        protected static void DetachNativeProvider()
        {
            // The manager stops itself on an update with no layers, which can run between SetUp and the test body.
            CompositionLayerManager.StartCompositionLayerManager();
            CompositionLayerManager.Instance.LayerProvider = null;
        }

        /// Puts the base class back in the native provider slot after DetachNativeProvider.
        protected void ReattachNativeProvider()
            => CompositionLayerManager.Instance.LayerProvider = this;

        /// Detaches the native provider and forgets it was ever set, as on a platform that never sets one.
        protected static void ForgetNativeProvider()
        {
            DetachNativeProvider();
            CompositionLayerManager.ResetLayerProviderHistory();
        }

        void ResetUpdateSignal()
        {
            m_UpdateCompletion = null;
            m_UpdateReported = false;
        }

        // Reset gives new layers QuadLayerData in Edit Mode only, so Play Mode tests assign it here.
        protected CompositionLayer CreateLayerGameObject(bool recycle = true, bool assignLayerType = true)
        {
            var name = "Test";
            GameObject gameObject = null;

            if (recycle)
            {
                gameObject = GameObject.Find(name);
            }

            if (gameObject == null)
            {
                name += Guid.NewGuid().ToString();
                gameObject = new GameObject(name);
                gameObject.SetActive(false);
            }

            var layerComponent = gameObject.AddComponent<CompositionLayer>();
            if (assignLayerType && layerComponent.LayerData == null)
                layerComponent.ChangeLayerDataType(typeof(QuadLayerData));

            return layerComponent;
        }

        protected static bool FindLayer(List<LayerInfo> layerInfos, CompositionLayer layer, out int index)
        {
            for (var i = 0; i < layerInfos.Count; i++)
            {
                if (layerInfos[i].Layer == layer)
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        // Creates a <see cref="CompositionLayer"/> of the layer data type,
        // optionally assigns an order, activates it and optionally adds a suggested extensions.
        protected CompositionLayer CreateActiveLayer(Type layerDataType = null, int? order = null, bool addSuggestedExtensions = false)
        {
            var layer = CreateLayerGameObject(false);

            if (layerDataType != null)
                layer.ChangeLayerDataType(layerDataType);

            if (order.HasValue)
                layer.Order = order.Value;

            layer.gameObject.SetActive(true);

            if (addSuggestedExtensions)
                layer.AddSuggestedExtensions();

            m_TrackedLayers.Add(layer); // Tracked for Teardown.
            return layer;
        }

        protected CompositionLayer CreateActiveLayer<T>(bool addSuggestedExtensions = false) where T : LayerData
            => CreateActiveLayer(typeof(T), null, addSuggestedExtensions);

        // Creates an inactive <see cref="CompositionLayer"/> of the layer data type
        // Useful for pure property/logic tests that do not need the layer to be registered with the manager.

        protected CompositionLayer CreateTrackedLayer(Type layerDataType = null)
        {
            var layer = CreateLayerGameObject(false);
            if (layerDataType != null)
                layer.ChangeLayerDataType(layerDataType);
            m_TrackedLayers.Add(layer);
            return layer;
        }

        /// Returns the extension component on the layer, or null.
        protected static T GetExtension<T>(CompositionLayer layer) where T : CompositionLayerExtension
            => layer.gameObject.GetComponent<T>();

        /// True if the layer was reported in the modified list during the last provider update cycle.
        protected bool WasReportedModified(CompositionLayer layer) => FindLayer(lastModifiedLayers, layer, out _);

        /// True if the layer was reported in the active list during the last provider update cycle.
        protected bool WasReportedActive(CompositionLayer layer) => FindLayer(lastActiveLayers, layer, out _);

        /// True if the layer was reported in the created list during the last provider update cycle.
        protected bool WasReportedCreated(CompositionLayer layer) => FindLayer(lastCreatedLayers, layer, out _);

        /// Treats the layer data type as passthrough, the way a platform's camera subsystem does at runtime.
        protected static void SetPassthroughLayerType(Type layerDataType)
            => CompositionLayerManager.PassthroughLayerType = layerDataType;

        /// Fails if any single update cycle dispatched the layer to both providers.
        protected void AssertDispatchedToOneProvider(CompositionLayer layer)
        {
            var cycles = Mathf.Min(m_NativeRecorder.Reports.Count, m_EmulationRecorder.Reports.Count);

            for (var i = 0; i < cycles; i++)
            {
                var native = RecordingLayerProvider.Owns(m_NativeRecorder.Reports[i], layer);
                var emulated = RecordingLayerProvider.Owns(m_EmulationRecorder.Reports[i], layer);

                Assert.IsFalse(native && emulated,
                    $"Layer '{layer.name}' was dispatched to both providers in update cycle {i}.");
            }
        }
    }
}
