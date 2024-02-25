using System;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Base class for all composition layer types. Derive from this and extend to add
    /// your own layer type.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Composition Layers/Composition Layer")]
    [Icon(CompositionLayerConstants.IconPath + "d_LayerUniversal.png")]
    [CompositionLayersHelpURL(typeof(CompositionLayer))]
    public sealed class CompositionLayer : MonoBehaviour
    {
#if UNITY_EDITOR
        readonly Type k_UIMirrorComponentType = System.Reflection.Assembly.Load("Unity.XR.CompositionLayers.UIInteraction").GetType("Unity.XR.CompositionLayers.UIInteraction.InteractableUIMirror");
#endif
        [SerializeField, HideInInspector]
        Canvas m_UICanvas;

        [SerializeField, HideInInspector]
        Component m_UIMirrorComponent;

        [SerializeField, HideInInspector]
        Component m_LayerOutline;

        [SerializeField]
        [Tooltip(@"The layer ordering of this layer in relation to the main eye layer.
            Order < 0 will render under the eye layer in ascending order.
            Order >= 0 will render over the eye layer in ascending order.")]
        int m_Order;

        [SerializeReference]
        [Tooltip("The data associated with the layer type this layer is set to.")]
        internal LayerData m_LayerData;

        /// <summary>
        /// Current PlatformLayerData cache. This property isn't serialized immediately.
        /// </summary>
        [NonSerialized]
        PlatformLayerData m_PlatformLayerData;
        /// <summary>
        /// Serialized keys for PlatformLayerData.
        /// </summary>
        [SerializeField]
        internal string[] m_PlatformLayerDataKeys;
        /// <summary>
        /// Serialized values for PlatformLayerData.
        /// </summary>
        [SerializeField]
        internal string[] m_PlatformLayerDataTexts;

#pragma warning disable 0414
        // Using a NonSerialized field to make sure the value is default after domain reload.
        [NonSerialized]
        bool m_OrderInitialized;

        /// <summary>
        /// Provides access to the list of <see cref="CompositionLayerExtension"/> components that are currently enabled on this CompositionLayer gameObject.
        /// </summary>
        public List<CompositionLayerExtension> Extensions => m_Extensions;
        List<CompositionLayerExtension> m_Extensions = new List<CompositionLayerExtension>();

        /// <summary>
        /// Order Initialized is used to track if the <see cref="Order"/> is initialized to a valid value.
        /// A <see cref="Order"/> has been initialized when the <see cref="CompositionLayer"/> is managed with
        /// the <see cref="CompositionLayerManager"/>.
        /// </summary>
        public bool OrderInitialized => m_OrderInitialized;
#pragma warning restore 0414

        /// <summary>
        /// The layer ordering of this layer in relation to the main eye layer. Order less than 0 will render under the eye
        /// layer in ascending order. Order greater than or equal to 0 will render over the eye layer in ascending order.
        /// </summary>
        public int Order
        {
            get
            {
                return m_Order;
            }
            set
            {
                // Skip checks if the object is not active
                // Assume this is an initial value that will get checked on Awake
                if (!isActiveAndEnabled)
                {
                    if (CompositionLayerManager.ManagerActive
                        && CompositionLayerManager.Instance.OccupiedLayers.TryGetValue(m_Order, out var occupied)
                        && occupied == this)
                    {
                        CompositionLayerManager.Instance.OccupiedLayers.Remove(m_Order);
                    }

                    m_Order = value;
                    return;
                }

                this.TryChangeLayerOrder(m_Order, value);
            }
        }

        internal void SetLayerOrderInternal(int value)
        {
            m_Order = UpdateValue(m_Order, value);
            m_OrderInitialized = true;
        }

        /// <summary>
        /// The data associated with the layer type this layer is set to.
        /// </summary>
        /// <value>ScriptableObject instance for layer data.</value>
        public LayerData LayerData
        {
            get => m_LayerData;
            internal set
            {
                m_LayerData = UpdateValue(m_LayerData, value);
                if (LayerData != null)
                    LayerData.ReportStateChange = ReportStateChange;
            }
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void Update()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                ReportStateChange();
            }
            if ((LayerData?.GetType() == typeof(ProjectionLayerData)) || (LayerData?.GetType() == typeof(ProjectionLayerRigData)))
                ReportStateChange();
        }

        /// <summary>
        /// Get/Desrialize PlatformLayerData.
        /// This function keeps deselized PlatformLayerData internally.
        /// </summary>
        /// <typeparam name="T">The type of PlatformLayerData changed to.</typeparam>
        /// <returns>PlatformLayerData.</returns>
        public T GetPlatformLayerData<T>() where T : PlatformLayerData
        {
            return GetPlatformLayerData(typeof(T)) as T;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Set/Serialize PlatformLayerData.
        /// </summary>
        /// <param name="platformLayerData">Target PlatformLayerData.</param>
        public void SetPlatformLayerData(PlatformLayerData platformLayerData)
        {
            m_PlatformLayerData = platformLayerData;
            SerializePlatformLayerData(platformLayerData);
        }
#endif

        /// <inheritdoc cref="MonoBehaviour"/>=
        void Awake()
        {
            
            // Apply the CompositionOutline component when the object is created
            // to handle drawing outlines for quad and cylinder layers
#if UNITY_EDITOR
            if(m_LayerOutline == null) m_LayerOutline = Undo.AddComponent(gameObject, typeof(CompositionOutline));
#endif
            // Setting up the instance of CompositionLayerManager can send message with creating a new game object
            // this is not allowed to be called from Awake
            if (!Application.isPlaying)
                return;

            // Deserialize platform layer data at least once.
            GetActivePlatformLayerData();

            InitializeLayerOrder();

            if (LayerData != null)
                LayerData.ReportStateChange = ReportStateChange;

            CompositionLayerManager.Instance?.CompositionLayerCreated(this);
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                InitializeLayerOrder();
                CompositionLayerManager.Instance?.CompositionLayerCreated(this);
            }

            CompositionLayerManager.Instance?.CompositionLayerEnabled(this);
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnDisable()
        {
            CompositionLayerManager.Instance?.CompositionLayerDisabled(this);
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnDestroy()
        {
            CompositionLayerManager.Instance?.CompositionLayerDestroyed(this);
            m_OrderInitialized = false;
        }

        /// <summary>
        /// Initializes the <see cref="Order"/> value of the <see cref="CompositionLayer"/> with the
        /// <see cref="CompositionLayerManager"/> setting the <see cref="Order"/> to an unoccupied value.
        ///
        /// If the <see cref="Order"/> value is already occupied in the <see cref="CompositionLayerManager"/> another valid
        /// <see cref="Order"/> will be assigned.
        /// </summary>
        internal void InitializeLayerOrder()
        {
            // Since used in delay call the object could have bee destroyed
            if (!this || OrderInitialized)
                return;

            if (CompositionLayerManager.IsLayerSceneValid(this))
                CompositionLayerManager.StartCompositionLayerManager();

            if (!this.CanChangeOrderTo(Order))
            {
                if (CompositionLayerManager.Instance.DefaultSceneCompositionLayer == this)
                    Order = 0;

                var preferOverlay = LayerData == null || CompositionLayerUtils.GetLayerDescriptor(LayerData.GetType()).PreferOverlay;

                // Only use `preferOverlay` when layer is first created or order is 0
                var newOrder = Order == 0 ? CompositionLayerManager.GetFirstUnusedLayer(preferOverlay)
                    : CompositionLayerManager.GetNextUnusedLayer(Order);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (!this.TryChangeLayerOrder(m_Order, newOrder) && !this.TryChangeLayerOrder(m_Order, m_Order))
                        this.TryChangeLayerOrder(m_Order, CompositionLayerManager.GetNextUnusedLayer(newOrder));
                }
#endif
                Order = newOrder;
            }
            else
            {
                CompositionLayerManager.Instance.OccupiedLayers.TryAdd(Order, this);
            }

            m_OrderInitialized = true;
        }

        /// <summary>
        /// Report a state change to the <see cref="CompositionLayerManager"/>
        /// </summary>
        internal void ReportStateChange()
        {
            if (isActiveAndEnabled && CompositionLayerManager.ManagerActive)
                CompositionLayerManager.Instance.CompositionLayerStateChanged(this);
        }

        /// <summary>
        /// Check if new value != old value. If it is, then report state change and return new value.
        /// Otherwise return old value
        /// </summary>
        /// <param name="oldValue">Current value to check for equality</param>
        /// <param name="newValue">The new value we want to change the old value to.</param>
        /// <typeparam name="T">Type of old and new value.</typeparam>
        /// <returns>Old value if new value is the same, otherwise the new value.</returns>
        T UpdateValue<T>(T oldValue, T newValue)
        {
            if (!(oldValue?.Equals(newValue) ?? false))
            {
                ReportStateChange();
                return newValue;
            }

            return oldValue;
        }

        /// <summary>
        /// Sets the <see cref="LayerData"/>.
        /// </summary>
        /// <param name="layer">The <see cref="LayerData"/> instance to assign.</param>
        public void ChangeLayerDataType(LayerData layer)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, Undo.GetCurrentGroupName());
#endif
            LayerData = layer;
        }

        /// <summary>
        /// Will populate the layer data with existing data associated with that <paramref name="typeFullName"/>.
        /// </summary>
        /// <param name="typeFullName">The layer Id for the <see cref="Layers.LayerData"/> type.</param>
        public void ChangeLayerDataType(string typeFullName)
        {
            ChangeLayerDataType(CompositionLayerUtils.CreateLayerData(typeFullName));
        }

        /// <summary>
        /// Sets the layer type from the <see cref="LayerDataDescriptor"/>. <see cref="LayerData"/> is populated from
        /// data associated with the <see cref="LayerDataDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="LayerDataDescriptor"/> for a type of <see cref="Layers.LayerData"/></param>
        public void ChangeLayerDataType(LayerDataDescriptor descriptor)
        {
            ChangeLayerDataType(CompositionLayerUtils.CreateLayerData(descriptor.DataType));
        }

        /// <summary>
        /// Sets the <see cref="LayerData"/>  base on a <see cref="LayerDataDescriptor"/> subclass of type T.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Layers.LayerData"/> to change the layer type to.</typeparam>
        public void ChangeLayerDataType<T>() where T : LayerData
        {
            ChangeLayerDataType(CompositionLayerUtils.GetLayerDescriptor(typeof(T)));
        }

        /// <summary>
        /// Sets the <see cref="LayerData"/> and base on a <see cref="LayerDataDescriptor"/> subclass defined
        /// by the passed in Type.
        /// </summary>
        /// <param name="type">The type of <see cref="Layers.LayerData"/> to change the layer type to.</param>
        public void ChangeLayerDataType(Type type)
        {
            ChangeLayerDataType(CompositionLayerUtils.GetLayerDescriptor(type));
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.DrawIcon(transform.position, CompositionLayerConstants.IconPath + "d_LayerUniversal_Gizmo.png");
#endif
        }

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnValidate()
        {
#if UNITY_EDITOR
            if (LayerData != null)
                LayerData.ReportStateChange = ReportStateChange;
#endif
        }

        ///<summary>
        /// Checks for changes in children to see if the user has childed or unchilded a canvas.
        /// Allows for swapping between a normal layer and a UI layer
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>=
        void OnTransformChildrenChanged()
        {
#if UNITY_EDITOR
            // Do not create a UI layer if not a Quad or Cylinder layer
            var layerDataType = LayerData?.GetType();
            if (layerDataType != typeof(QuadLayerData) && layerDataType != typeof(CylinderLayerData))
                return;

            var canvasChild = transform.GetComponentInChildren<Canvas>();

            // Make sure the child changed was the canvas
            if (m_UICanvas != canvasChild)
            {
                if (m_UIMirrorComponent != null)
                    Undo.DestroyObjectImmediate(m_UIMirrorComponent);

                // If there is no canvas, remove UI references
                if (canvasChild == null)
                {
                    m_UIMirrorComponent = null;
                    m_UICanvas = null;
                }
                // If there is a canvas, add UI references
                else
                {
                    Undo.RecordObject(this, "Cache canvas child.");
                    m_UICanvas = canvasChild;
                    m_UIMirrorComponent = Undo.AddComponent(gameObject, k_UIMirrorComponentType);
                }
            }
#endif
        }

        PlatformLayerData GetActivePlatformLayerData()
        {
            return GetPlatformLayerData(PlatformManager.ActivePlatformProvider?.PlatformLayerDataType);
        }

        PlatformLayerData GetPlatformLayerData(Type platformLayerDataType)
        {
            if (platformLayerDataType == null)
                return null;

            if (m_PlatformLayerData != null && m_PlatformLayerData.GetType() == platformLayerDataType)
                return m_PlatformLayerData;

            m_PlatformLayerData = DeserializePlatformLayerData(platformLayerDataType);
            return m_PlatformLayerData;
        }

#if UNITY_EDITOR
        void SerializePlatformLayerData(PlatformLayerData platformLayerData)
        {
            if (platformLayerData == null)
                return;

            var keys = m_PlatformLayerDataKeys;
            var texts = m_PlatformLayerDataTexts;

            var fullName = platformLayerData.GetType().FullName;
            int length = keys != null && texts != null ? Math.Min(keys.Length, texts.Length) : 0;
            for (int i = 0; i < length; ++i)
            {
                if (keys[i] == fullName)
                {
                    texts[i] = platformLayerData.Serialize();
                    return;
                }
            }

            Array.Resize(ref m_PlatformLayerDataKeys, length + 1);
            Array.Resize(ref m_PlatformLayerDataTexts, length + 1);

            m_PlatformLayerDataKeys[length] = fullName;
            m_PlatformLayerDataTexts[length] = platformLayerData.Serialize();
        }
#endif

        PlatformLayerData DeserializePlatformLayerData(Type platformLayerDataType)
        {
            var keys = m_PlatformLayerDataKeys;
            var texts = m_PlatformLayerDataTexts;
            if (platformLayerDataType == null || keys == null || texts == null)
                return null;

            var platformLayerData = Activator.CreateInstance(platformLayerDataType) as PlatformLayerData;
            if (platformLayerData == null)
                return null;

            var fullName = platformLayerDataType.FullName;
            int length = Math.Min(keys.Length, texts.Length);
            for (int i = 0; i < length; ++i)
            {
                if (keys[i] == fullName)
                {
                    platformLayerData.Deserialize(texts[i]);
                    return platformLayerData;
                }
            }

            platformLayerData.Deserialize(null);
            return platformLayerData;
        }
    }
}
