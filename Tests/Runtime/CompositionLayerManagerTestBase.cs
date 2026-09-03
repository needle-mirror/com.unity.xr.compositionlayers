using System;
using NUnit.Framework;
using System.Collections.Generic;
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

        bool m_EditorProviderState;

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

            CompositionLayerManager.Instance?.ClearAllState();
            CompositionLayerManager.Instance?.EnsureSceneCompositionManager();
        }

        protected virtual void Teardown()
        {
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
#if UNITY_6000_4_OR_NEWER
            var sceneLayers = UnityObject.FindObjectsByType<CompositionLayer>(FindObjectsInactive.Include);
#else
            var sceneLayers = UnityObject.FindObjectsByType<CompositionLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
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
        }

        public void LateUpdate() { }

        public void OnRenderObject() { }

        protected CompositionLayer CreateLayerGameObject(bool recycle = true)
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
    }
}
