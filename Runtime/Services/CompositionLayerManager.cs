using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CoreUtils;
using UnityObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers.Services
{
    /// <summary>
    /// Singleton manger for defined composition layers and
    /// updating layer information for a given <see cref="ILayerProvider" /> instance.
    ///
    /// The expected lifecycle of a layer in relation to the manager is as follows:
    /// | Composition Layer | Manager | Reported State |
    /// | -- | -- | -- |
    /// | Awake | <see cref="CompositionLayerCreated" /> | Created |
    /// | OnEnable | <see cref="CompositionLayerEnabled" /> | Modified, Active |
    /// | OnDisable | <see cref="CompositionLayerDisabled" /> | Modified |
    /// | OnDestroy | <see cref="CompositionLayerDestroyed" /> | Removed |
    ///
    ///
    /// The manager will report the set of created, removed, modified and active layers to the
    /// <see cref="s_LayerProvider" /> instance on every Update call. These lists are
    /// defined to contain layers a follows:
    ///
    /// **Created** : Any layer that has just been created. Populated on calls to <see cref="CompositionLayerCreated" />.
    ///
    /// This list is ephemeral and is cleared after each call to the layer provider.
    ///
    /// **Removed** : Any layer that has been destroyed will cause a call to
    /// <see cref="CompositionLayerDestroyed" />. The layer will be removed from the
    /// created, active and modified lists and added to the removed list.
    ///
    /// This list is ephemeral and is cleared after each call to the layer provider.
    ///
    /// **Modified** : Any layer that has changed in some way be added to this list. A modification could
    /// be a property change, or the layer being re-activated or de-activated. A layer is
    /// only added to this list if it isn't already in the Created or Removed lists.
    ///
    /// This list is ephemeral and is cleared after each call to the layer provider.
    ///
    /// A layer will only exist in one of Removed, Created or Modified on any call to the <see cref="s_LayerProvider" />.
    ///
    /// **Active** : This list contains the current set of active layers for this update call to
    /// the <see cref="s_LayerProvider" />. Layers passed to <see cref="CompositionLayerEnabled" /> will
    /// be added to this list, and layers passed to <see cref="CompositionLayerDisabled" /> or
    /// <see cref="CompositionLayerDestroyed" /> will be removed from this list.
    /// </summary>
    public sealed class CompositionLayerManager
    {
        /// <summary>
        /// Information about composition layers registered with the manager.
        /// </summary>
        public struct LayerInfo
        {
            /// <summary>
            /// Unique manager instance id for the registered layer.
            /// </summary>
            public int Id;

            /// <summary>
            /// The actual layer instance.
            /// </summary>
            public CompositionLayer Layer;
        }

        static CompositionLayerManager s_Instance = null;

        /// <summary>
        /// Singleton instance of <see cref="CompositionLayerManager" />.
        /// </summary>
        /// <value>Singleton instance of the <see cref="CompositionLayerManager" /></value>
        public static CompositionLayerManager Instance
        {
            get
            {
                if (s_ManagerStopped)
                    return null;

                if (s_Instance == null || s_ComponentInstance == null)
                    StartCompositionLayerManager();

                return s_Instance;
            }
        }

        static ILayerProvider s_LayerProvider;
        static ILayerProvider s_EmulationLayerProvider;
        static CallbackComponent s_ComponentInstance;
        static CompositionLayer s_FallbackDefaultSceneCompositionLayer;
        static CompositionLayer s_DefaultSceneCompositionLayer;
        static bool s_ManagerStopped;

        internal static Action OccupiedLayersUpdated;
        internal static Action ManagerStarted;
        internal static Action ManagerStopped;

        internal CompositionLayer FallbackDefaultSceneCompositionLayer => s_FallbackDefaultSceneCompositionLayer;
        /// <summary>
        /// The <see cref="CompositionLayer"/> that is being used to render the default scene layer in a composition.
        /// </summary>
        public CompositionLayer DefaultSceneCompositionLayer => s_DefaultSceneCompositionLayer;

        readonly Dictionary<CompositionLayer, LayerInfo> m_KnownLayers = new Dictionary<CompositionLayer, LayerInfo>();

        readonly List<LayerInfo> m_CreatedLayers = new List<LayerInfo>();
        readonly List<int> m_RemovedLayers = new List<int>();
        readonly List<LayerInfo> m_ModifiedLayers = new List<LayerInfo>();
        readonly List<LayerInfo> m_ActiveLayers = new List<LayerInfo>();
        internal readonly Dictionary<int, CompositionLayer> OccupiedLayers = new Dictionary<int, CompositionLayer>();
        internal bool OccupiedLayersDirty;

        ProjectionRigOffsetSynchronizer m_ProjectionRigOffsetSynchronizer;

        /// <summary>
        /// The currently assigned <see cref="ILayerProvider" /> instance that this manager
        /// instance should talk to.
        /// </summary>
        public ILayerProvider LayerProvider
        {
            get => s_LayerProvider;
            set
            {
                if (s_LayerProvider != value)
                {
                    s_LayerProvider?.CleanupState();

                    s_LayerProvider = value;

                    s_LayerProvider?.SetInitialState(m_KnownLayers.Values.ToList());
                }
            }
        }

        /// <summary>
        /// The currently assigned <see cref="ILayerProvider" /> emulation provider instance that this manager
        /// instance should talk to.
        /// </summary>
        public ILayerProvider EmulationLayerProvider
        {
            get => s_EmulationLayerProvider;
            set
            {

                if (s_EmulationLayerProvider != value)
                {
                    s_EmulationLayerProvider?.CleanupState();

                    s_EmulationLayerProvider = value;

                    s_EmulationLayerProvider?.SetInitialState(m_KnownLayers.Values.ToList());
                }
            }
        }

        internal static bool ManagerActive => s_Instance != null;

        internal static void StartCompositionLayerManager()
        {
            // Ensures manager can be started from external script without
            // needing to directly call the 'instance' from that script.s
            if (s_Instance == null)
            {
                s_ManagerStopped = false;
                s_Instance = new CompositionLayerManager();
                s_Instance.m_ProjectionRigOffsetSynchronizer = new ProjectionRigOffsetSynchronizer();
            }

            if (s_ComponentInstance == null)
                s_Instance.EnsureSceneCompositionManager();

            ManagerStarted?.Invoke();
        }

        internal static void StopCompositionLayerManager()
        {
            if (s_ManagerStopped)
                return;

            s_ManagerStopped = true;

            if (s_Instance != null)
            {
                s_Instance.ClearAllState();
                s_Instance = null;
            }

            if (s_ComponentInstance != null)
            {
                s_ComponentInstance.Awoke = null;
                s_ComponentInstance.Updated = null;
                s_ComponentInstance.LateUpdated = null;
                s_ComponentInstance.Destroyed = null;

                UnityObjectUtils.Destroy(s_ComponentInstance.gameObject);
                s_ComponentInstance = null;
            }

            if (s_FallbackDefaultSceneCompositionLayer != null)
            {
                UnityObjectUtils.Destroy(s_FallbackDefaultSceneCompositionLayer.gameObject);
                s_FallbackDefaultSceneCompositionLayer = null;
            }

            ManagerStopped?.Invoke();
        }

        internal void EnsureSceneCompositionManager()
        {
            var sceneGameObject = GameObject.Find(CompositionLayerConstants.SceneManagerName);

            if (sceneGameObject == null)
            {
                sceneGameObject = new GameObject(CompositionLayerConstants.SceneManagerName);
                sceneGameObject.hideFlags = HideFlags.HideAndDontSave;
                s_ComponentInstance = null;
            }

            sceneGameObject.SetActive(false);

            s_ComponentInstance = sceneGameObject.GetComponent<CallbackComponent>();
            if (s_ComponentInstance == null)
                s_ComponentInstance = sceneGameObject.AddComponent<CallbackComponent>();

            // Using assignments since send message cannot be called in awake
            s_ComponentInstance.Awoke = Awake;
            s_ComponentInstance.Updated = Update;
            s_ComponentInstance.LateUpdated = LateUpdate;
            s_ComponentInstance.Destroyed = OnSceneComponentDestroyed;

            EnsureFallbackSceneCompositionLayer();
            sceneGameObject.SetActive(true);
        }

        internal void EnsureFallbackSceneCompositionLayer()
        {
            GameObject sceneGameObject;

            if (s_FallbackDefaultSceneCompositionLayer != null && s_FallbackDefaultSceneCompositionLayer.gameObject != null)
            {
                sceneGameObject = s_FallbackDefaultSceneCompositionLayer.gameObject;
            }
            else
            {
                sceneGameObject = GameObject.Find(CompositionLayerConstants.DefaultSceneLayerName);

                if (sceneGameObject == null)
                {
                    sceneGameObject = new GameObject(CompositionLayerConstants.DefaultSceneLayerName);
                    sceneGameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            s_FallbackDefaultSceneCompositionLayer = sceneGameObject.GetComponent<CompositionLayer>();
            if (s_FallbackDefaultSceneCompositionLayer == null)
                s_FallbackDefaultSceneCompositionLayer = sceneGameObject.AddComponent<CompositionLayer>();

            if (s_FallbackDefaultSceneCompositionLayer.LayerData == null)
            {
                s_FallbackDefaultSceneCompositionLayer.LayerData = CompositionLayerUtils.CreateLayerData(typeof(DefaultLayerData).FullName);
            }

            if (s_DefaultSceneCompositionLayer == null)
                SetDefaultSceneCompositionLayer(s_FallbackDefaultSceneCompositionLayer);

            OccupiedLayersDirty = true;
            OccupiedLayersUpdated?.Invoke();
        }

        static void OnSceneComponentDestroyed()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                Instance.ClearAllState();
                if (s_ComponentInstance == null)
                    Instance.EnsureSceneCompositionManager();
                Instance.FindAllLayersInScene();
            };
#endif
        }

        internal void ClearAllState()
        {
            m_KnownLayers.Clear();
            m_CreatedLayers.Clear();
            m_RemovedLayers.Clear();
            m_ModifiedLayers.Clear();
            m_ActiveLayers.Clear();
            OccupiedLayers.Clear();
        }

        internal void ClearSingleShotState()
        {
            m_CreatedLayers.Clear();
            m_RemovedLayers.Clear();
            m_ModifiedLayers.Clear();
        }

        static int LayerSorter(LayerInfo lhs, LayerInfo rhs)
        {
            if (lhs.Layer.Order == rhs.Layer.Order)
                return lhs.Id.CompareTo(rhs.Id);

            if (lhs.Layer.Order < rhs.Layer.Order)
                return -1;

            return 1;
        }

        /// <summary>
        /// Called to report that a new instance of a <see cref="CompositionLayer" /> is
        /// created. By default this is called from calls to Awake on a
        /// <see cref="CompositionLayer" /> instance.
        /// </summary>
        /// <param name="layer">New layer to add to management.</param>
        public void CompositionLayerCreated(CompositionLayer layer)
        {
            if (!IsLayerSceneValid(layer))
                return;

            var layerKnown = m_KnownLayers.ContainsKey(layer);
            if (layerKnown || !OccupiedLayers.TryGetValue(layer.Order, out var occupiedLayer))
            {
                OccupiedLayers.TryAdd(layer.Order, layer);
                OccupiedLayersUpdated?.Invoke();
            }
            else if (occupiedLayer != layer)
            {
                var orderInitialized = layer.OrderInitialized;
                if (orderInitialized)
                    CompositionLayerUtils.LogLayerOrderCannotBeSet(layer, layer.Order);

                var order = GetNextUnusedLayer(layer.Order);
                layer.Order = order;
                if (orderInitialized)
                    Debug.Log($"{layer.gameObject.name} is set to next available Layer Order: {order.ToString()}.");
            }

            if (layerKnown)
                return;

            var li = new LayerInfo() { Layer = layer, Id = layer.GetInstanceID() };
            m_KnownLayers.Add(layer, li);
            m_CreatedLayers.Add(li);
        }

        /// <summary>
        /// Called to report that an instance of a <see cref="CompositionLayer" /> is
        /// active and ready to be rendered. By default this is called from calls to
        /// OnEnable on a <see cref="CompositionLayer" /> instance.
        ///
        /// When called with a layer that is not currently active, the layer will be added
        /// to the active list as well as the added list.
        ///
        /// If the manager doesn't know about this layer (i.e. the layer instance was
        /// not passed to <see cref="CompositionLayerCreated" /> previously) then the
        /// layer is ignored.
        /// </summary>
        /// <param name="layer">Currently managed layer to set to active.</param>
        public void CompositionLayerEnabled(CompositionLayer layer)
        {
            if (!IsLayerSceneValid(layer))
                return;

            if (!m_KnownLayers.ContainsKey(layer))
                CompositionLayerCreated(layer);

            var li = m_KnownLayers[layer];

            if (!m_CreatedLayers.Contains(li) && !m_ModifiedLayers.Contains(li))
                m_ModifiedLayers.Add(li);

            if (!m_ActiveLayers.Contains(li))
                m_ActiveLayers.Add(li);

            if (!OccupiedLayers.TryGetValue(layer.Order, out _))
            {
                OccupiedLayers.Add(layer.Order, layer);
                OccupiedLayersUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Called to report that an instance of a <see cref="CompositionLayer" /> is
        /// not active and should not be rendered. By default this is called from calls to
        /// OnDisable on a <see cref="CompositionLayer" /> instance.
        ///
        /// When called with a layer that is active, the layer will be removed from the
        /// active list.
        ///
        /// If the manager doesn't know about this layer (i.e. the layer instance was
        /// not passed to <see cref="CompositionLayerCreated" /> previously) then the
        /// layer is ignored.
        /// </summary>
        /// <param name="layer">Currently managed layer to set to disabled.</param>
        public void CompositionLayerDisabled(CompositionLayer layer)
        {
            if (!m_KnownLayers.ContainsKey(layer))
                return;

            var li = m_KnownLayers[layer];
            if (m_ActiveLayers.Contains(li))
                m_ActiveLayers.Remove(li);

            if (!m_ModifiedLayers.Contains(li))
                m_ModifiedLayers.Add(li);
        }

        /// <summary>
        /// Called to report that an instance of a <see cref="CompositionLayer" /> is
        /// being destroyed and or should be removed from management. By default this
        /// is called from calls to OnDestroy on a <see cref="CompositionLayer" /> instance.
        ///
        /// When called the layer will be added to the removed layer list. If layer is
        /// currently active, the layer will be removed from the active list as well.
        ///
        /// If the manager doesn't know about this layer (i.e. the layer instance was
        /// not passed to <see cref="CompositionLayerCreated" /> previously) then the
        /// layer is ignored.
        /// </summary>
        /// <param name="layer">Currently managed layer to remove from management.</param>
        public void CompositionLayerDestroyed(CompositionLayer layer)
        {
            if (m_KnownLayers.ContainsKey(layer))
            {
                var li = m_KnownLayers[layer];
                m_KnownLayers.Remove(layer);
                if (m_CreatedLayers.Contains(li))
                    m_CreatedLayers.Remove(li);
                if (m_ActiveLayers.Contains(li))
                    m_ActiveLayers.Remove(li);
                if (m_ModifiedLayers.Contains(li))
                    m_ModifiedLayers.Remove(li);
                m_RemovedLayers.Add(li.Id);

                if (layer.LayerData?.GetType() == typeof(ProjectionLayerRigData))
                    m_ProjectionRigOffsetSynchronizer.RemoveProjectionRig(layer.GetInstanceID());
            }

            if (OccupiedLayers.TryGetValue(layer.Order, out var occupiedLayer) && occupiedLayer == layer)
            {
                OccupiedLayers.Remove(layer.Order);
                OccupiedLayersUpdated?.Invoke();
            }

            CheckIfShutDownManager();
        }

        /// <summary>
        /// Report a change in state/data for a layer. This could be direct layer state
        /// changes or it could be due to changes in data on extension components for this layer.
        /// </summary>
        /// <param name="layer">The <see cref="CompositionLayer"/> that is modified.</param>
        public void CompositionLayerStateChanged(CompositionLayer layer)
        {
            if (layer == null || !m_KnownLayers.ContainsKey(layer))
                return;

            var li = m_KnownLayers[layer];

            if (!m_CreatedLayers.Contains(li) && !m_RemovedLayers.Contains(li.Id) && !m_ModifiedLayers.Contains(li))
            {
                m_ModifiedLayers.Add(li);

                if (layer.LayerData?.GetType() == typeof(ProjectionLayerRigData))
                    m_ProjectionRigOffsetSynchronizer.AddProjectionRig(layer);
                else
                    // remove layer that may have previously been a projection rig layer.
                    m_ProjectionRigOffsetSynchronizer.RemoveProjectionRig(layer.GetInstanceID());
            }
        }

        internal void FindAllLayersInScene()
        {
            var isPlaying = Application.isPlaying;
            if (!isPlaying)
                ClearAllState();
            else
                OccupiedLayers.Clear();

            if (DefaultSceneCompositionLayer == s_FallbackDefaultSceneCompositionLayer)
            {
                CompositionLayerCreated(DefaultSceneCompositionLayer);
                CompositionLayerEnabled(DefaultSceneCompositionLayer);
            }

            var foundLayers = UnityObject.FindObjectsOfType<CompositionLayer>(!isPlaying);
            foreach (var layer in foundLayers)
            {
                if (!IsLayerSceneValid(layer))
                    continue;

                CompositionLayerCreated(layer);
                if (layer.enabled && layer.gameObject.activeInHierarchy)
                    CompositionLayerEnabled(layer);
            }

            OccupiedLayersUpdated?.Invoke();
        }

        void Awake()
        {
            ClearAllState();
            FindAllLayersInScene();
        }

        internal void Update()
        {
            if (DefaultSceneCompositionLayer == null || !DefaultSceneCompositionLayer.isActiveAndEnabled)
                ResetDefaultSceneCompositionLayer();

            // This is to for when deactivated game objects are deleted in the editor
            if (OccupiedLayersDirty)
            {
#if UNITY_EDITOR
                FindAllLayersInScene();
#endif
                OccupiedLayersDirty = false;
            }

            if (s_LayerProvider != null)
            {
                m_ActiveLayers.Sort(LayerSorter);
                s_LayerProvider.UpdateLayers(m_CreatedLayers, m_RemovedLayers, m_ModifiedLayers, m_ActiveLayers);
            }
            if (s_EmulationLayerProvider != null)
            {
                m_ActiveLayers.Sort(LayerSorter);
                s_EmulationLayerProvider.UpdateLayers(m_CreatedLayers, m_RemovedLayers, m_ModifiedLayers,
                    m_ActiveLayers);
            }

            ClearSingleShotState();
        }

        internal void LateUpdate()
        {
            s_LayerProvider?.LateUpdate();
            s_EmulationLayerProvider?.LateUpdate();
            m_ProjectionRigOffsetSynchronizer.SyncRigsWithMainCameraParentOffsets();
        }

        internal static void GetOccupiedLayers(List<CompositionLayer> layers)
        {
            layers.Clear();

            if (ManagerActive)
                layers.AddRange(Instance.OccupiedLayers.Values);
        }

        internal static bool IsLayerSceneValid(CompositionLayer layer)
        {
            // Default layer is a free game object and not part of a normal scene
            if (layer == s_FallbackDefaultSceneCompositionLayer)
                return true;

            if (!layer.gameObject.scene.IsValid())
                return false;

#if UNITY_EDITOR
            // Check if the layer is being created in the active scene
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (layer.gameObject.scene == SceneManager.GetSceneAt(i))
                    return true;
            }

            // Do not manage layers in prefab isolation stage
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.IsValid() && stage.scene == layer.gameObject.scene)
                return false;
#endif

            return true;
        }

        /// <summary>
        /// Get the first unoccupied <see cref="CompositionLayer.Order"/> value in the currently open scenes.
        /// </summary>
        /// <param name="overlay">
        /// If <code>true</code> the first unoccupied order value greater than 0 is returned. If <code>false</code> the first
        /// unoccupied order value less than 0 is returned.
        /// </param>
        /// <returns>Returns the first unoccupied order value order value.</returns>
        public static int GetFirstUnusedLayer(bool overlay)
        {
            return GetNextUnusedLayer(0, overlay);
        }

        /// <summary>
        /// Get the first unoccupied layer <see cref="CompositionLayer.Order"/> value that is greater than 0 in the currently
        /// open scenes.
        /// </summary>
        /// <returns>Returns the first unoccupied order value order value.</returns>
        public static int GetFirstUnusedLayer()
        {
            return GetNextUnusedLayer(0, true);
        }

        /// <summary>
        /// Gets the next unoccupied layer <see cref="CompositionLayer.Order"/> value that is greater than <paramref name="order"/>
        /// if the value is positive or less than if the value is negative.
        /// </summary>
        /// <param name="order">The order value to get the next unoccupied value for.</param>
        /// <returns>
        /// The next unoccupied layer <see cref="CompositionLayer.Order"/> value that is greater than <paramref name="order"/>
        /// if the value is positive or less than if the value is negative.
        /// </returns>
        public static int GetNextUnusedLayer(int order)
        {
            return GetNextUnusedLayer(order, order > -1);
        }

        /// <summary>
        /// Gets the next unoccupied layer <see cref="CompositionLayer.Order"/> value that is greater than or less than
        /// <paramref name="order"/>.
        /// </summary>
        /// <param name="order">The order value to get the next unoccupied value for.</param>
        /// <param name="overlay">
        /// If <code>true</code> the first unoccupied order value greater than <paramref name="order"/> is returned.
        /// If <code>false</code> the first unoccupied order value less than <paramref name="order"/> is returned.
        /// </param>
        /// /// <returns>
        /// The next unoccupied layer <see cref="CompositionLayer.Order"/> value that is greater than <paramref name="order"/>
        /// if the value is positive or less than if the value is negative.
        /// </returns>
        public static int GetNextUnusedLayer(int order, bool overlay)
        {
            if (order == 0)
                order = overlay ? 1 : -1;

            if (!ManagerActive)
                return order;

            while (Instance.OccupiedLayers.ContainsKey(order) || order == 0)
            {
                if (overlay)
                    order++;
                else
                    order--;
            }

            return order;
        }

        /// <summary>
        /// Sets the <see cref="CompositionLayer"/> to be used as the <see cref="DefaultSceneCompositionLayer"/>. This
        /// will unset and disable the previous <see cref="DefaultSceneCompositionLayer"/>. If the
        /// <see cref="DefaultSceneCompositionLayer"/> fails to be set to the <see cref="CompositionLayer"/> a fallback
        /// will be used.
        /// </summary>
        /// <param name="compositionLayer">The layer to use as the <see cref="DefaultSceneCompositionLayer"/>.</param>
        public void SetDefaultSceneCompositionLayer(CompositionLayer compositionLayer)
        {
            if (compositionLayer != null
                && DefaultSceneCompositionLayer == compositionLayer
                && compositionLayer.Order == 0
                && OccupiedLayers.TryGetValue(0, out var otherLayer)
                && otherLayer == compositionLayer)
                return;

            if (DefaultSceneCompositionLayer != compositionLayer)
                UnsetPreviousDefaultSceneCompositionLayer();

            if (compositionLayer == null)
            {
                EnsureFallbackSceneCompositionLayer();
                return;
            }


            var parentTransform = compositionLayer.gameObject.transform.parent;
            if (!compositionLayer.gameObject.activeInHierarchy && parentTransform != null && !parentTransform.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"Cannot use {compositionLayer.gameObject.name} as default scene layer! " +
                    "Cannot activate the GameObject in the hierarchy.");
                s_DefaultSceneCompositionLayer = s_FallbackDefaultSceneCompositionLayer;
                EnsureFallbackSceneCompositionLayer();
                return;
            }

            s_DefaultSceneCompositionLayer = compositionLayer;

            compositionLayer.Order = 0;
            compositionLayer.gameObject.SetActive(true);
            compositionLayer.enabled = true;

            OccupiedLayersDirty = true;
            OccupiedLayersUpdated?.Invoke();
        }

        /// <summary>
        /// Reset the <see cref="DefaultSceneCompositionLayer"/> back to the fallback <see cref="CompositionLayer"/>
        /// provided byt the <see cref="CompositionLayerManager"/>.
        /// </summary>
        public void ResetDefaultSceneCompositionLayer()
        {
            if (DefaultSceneCompositionLayer == s_FallbackDefaultSceneCompositionLayer)
            {
                EnsureFallbackSceneCompositionLayer();
                return;
            }

            SetDefaultSceneCompositionLayer(null);
        }

        void UnsetPreviousDefaultSceneCompositionLayer()
        {
            var previousSceneLayer = s_DefaultSceneCompositionLayer;
            s_DefaultSceneCompositionLayer = null;

            if (previousSceneLayer == null)
                return;

            if (previousSceneLayer.Order == 0 && OccupiedLayers.ContainsKey(0) && OccupiedLayers[0] == previousSceneLayer)
            {
                OccupiedLayers.Remove(0);
                previousSceneLayer.enabled = false;
                Debug.Log($"{previousSceneLayer.gameObject}'s is no longer the Default Scene Composition Layer and is now disabled.");
            }

            if (s_FallbackDefaultSceneCompositionLayer != previousSceneLayer &&
                (previousSceneLayer.Order == 0 || !previousSceneLayer.CanChangeOrderTo(previousSceneLayer.Order)))
            {
                var previousSceneLayerData = previousSceneLayer.LayerData;
                var preferOverlay = previousSceneLayerData == null
                    || CompositionLayerUtils.GetLayerDescriptor(previousSceneLayerData.GetType()).PreferOverlay;

                previousSceneLayer.Order = GetFirstUnusedLayer(preferOverlay);
            }
        }

        void CheckIfShutDownManager()
        {
            if (OccupiedLayers.Count == 1 && OccupiedLayers.TryGetValue(0, out var occupiedLayer)
                && occupiedLayer == s_FallbackDefaultSceneCompositionLayer)
            {
                // Update for the newly removed layers before stopping.
                if (m_RemovedLayers.Count > 0) 
                    Update();

                StopCompositionLayerManager();
            }
        }

        // This class syncs the transforms of the projection rig composition layers to be at the same total offset of the main camera's parents.
        private class ProjectionRigOffsetSynchronizer
        {
            private Dictionary<int, Transform> projectionRigs = new Dictionary<int, Transform>();
            private Transform mainCameraTransform;

            public ProjectionRigOffsetSynchronizer()
            {
                mainCameraTransform = Camera.main?.transform;
            }

            public void AddProjectionRig(CompositionLayer projectionRig)
            {
                // Early out if this rig's transform has already been added
                if (projectionRigs.ContainsKey(projectionRig.GetInstanceID()))
                    return;

                // Add the rig to the dictionary and immediately sync it's transform with the main camera's parents.
                var rigTransform = projectionRig.transform;
                projectionRigs[projectionRig.GetInstanceID()] = rigTransform;
                var totalParentOffset = GetTotalLocalPoseOffsetOfMainCameraParents();
                rigTransform.SetWorldPose(totalParentOffset);
            }

            public void RemoveProjectionRig(int rigId)
            {
                projectionRigs.Remove(rigId);
            }

            public void SyncRigsWithMainCameraParentOffsets(bool forceSync = false)
            {
                // Early out if none of the main camera's parents have had their transforms changed.
                if (!forceSync && !ParentsHaveChanged())
                    return;

                // Sync all rig transforms with the main camera's parents.
                var totalParentOffset = GetTotalLocalPoseOffsetOfMainCameraParents();
                foreach (Transform projectionRig in projectionRigs.Values)
                {
                    projectionRig.SetWorldPose(totalParentOffset);
                }
            }

            bool ParentsHaveChanged()
            {
                if (mainCameraTransform == null)
                    mainCameraTransform = Camera.main?.transform;

                bool parentsHaveChanged = false;
                var currentParent = mainCameraTransform?.parent;

                // Loop through all of the main camera's parents and report if any have changed.
                while (currentParent != null)
                {
                    if (!parentsHaveChanged)
                        parentsHaveChanged = currentParent.hasChanged;

                    // Must reset hasChanged to false.
                    currentParent.hasChanged = false;
                    currentParent = currentParent.parent;
                }

                return parentsHaveChanged;
            }

            Pose GetTotalLocalPoseOffsetOfMainCameraParents()
            {
                if (mainCameraTransform == null)
                    mainCameraTransform = Camera.main?.transform;

                var totalLocalPoseOffset = Pose.identity;
                Transform currentParent = mainCameraTransform?.parent;

                // Loop through all of the main camera's parents and keep a running total of their local poses.
                while (currentParent != null)
                {
                    var parentLocalPose = currentParent.GetLocalPose();
                    totalLocalPoseOffset = new Pose(totalLocalPoseOffset.position + parentLocalPose.position, totalLocalPoseOffset.rotation * parentLocalPose.rotation);
                    currentParent = currentParent.parent;
                }

                return totalLocalPoseOffset;
            }
        }
    }
}
