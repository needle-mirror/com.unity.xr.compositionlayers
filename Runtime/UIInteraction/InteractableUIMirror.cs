using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Extensions;
#if !UNITY_EDITOR
using Unity.XR.CompositionLayers.Services;
#endif
#if UNITY_XR_INTERACTION_TOOLKIT
using System;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
#endif
#endif

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Handles Canvas and Composition Layer Syncing (Size, Scale, etc)
    /// and transforming raycasts from the Composition Layer to the attached Canvas
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CompositionLayer), typeof(TexturesExtension))]
    public class InteractableUIMirror : MonoBehaviour
    {
        // Minimum size for the canvas to prevent it from being too small
        const float MINIMUM_CANVAS_SIZE = 0.001f;

#if UNITY_XR_INTERACTION_TOOLKIT
        struct ProxyHoverBinding
        {
            public IUIHoverInteractor proxyHoverInteractor;
            public IUIHoverInteractor realHoverInteractor;
            public bool isProxyHoveringUI;
        }
        // reference to the Interactable for XRI interaction
        [SerializeField, HideInInspector]
        XRSimpleInteractable xrSimpleInteractable;

        // Reference to the camera that will be used to display the UI
        [SerializeField, HideInInspector]
        Camera canvasCamera;

        // Reference to the render texture the UI camera will render to
        [SerializeField, HideInInspector]
        RenderTexture canvasCameraRenderTexture;

        // Reference to the Tracked DeviceGraphic Raycaster for XRI interaction
        [SerializeField, HideInInspector]
        TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster;

        // Reference to the mesh collider for raycasting
        [SerializeField, HideInInspector]
        MeshCollider meshCollider;

        // Reference to this object's UIHandle and UIFocus
        [SerializeField, HideInInspector]
        RectTransformData compositionLayerRectTransformData;

        // Reference to either a QuadUIScale or CylinderUIScale to handle colliders
        [SerializeField, HideInInspector]
        LayerUIScale layerUIScale;

        // Reference to a canvas group which allows for raycast control
        [SerializeField, HideInInspector]
        CanvasGroup canvasGroup;

        // Reference to the texture extension that the camera render texture will hook into
        [SerializeField, HideInInspector]
        TexturesExtension texturesExtension;

        // Reference to the composition layer the UI will be mirroring to
        [SerializeField, HideInInspector]
        CompositionLayer compositionLayer;

        // Reference to the RectTransform of the canvas
        [SerializeField, HideInInspector]
        RectTransform canvasRectTransform;

        List<IXRRayProvider> interactors = new List<IXRRayProvider>();
        Canvas canvas;
        ProxyInteractorFactory proxyInteractorFactory;
        CanvasHitCalculator canvasHitCalculator;

        readonly Dictionary<IXRRayProvider, ProxyHoverBinding> proxyHoverBindings = new();

        CanvasAndCameraSynchronizer canvasAndCameraSynchronizer;
        CameraTargetTextureFactory cameraTargetTextureFactory;
        CanvasLayerCameraController canvasLayerCameraController;

        /// <summary>
        /// Data for each RectTransform in the hierarchy, used to add components if they don't exist
        /// </summary>
        [Serializable]
        sealed class RectTransformData
        {
            public UIHandle m_UIHandle;
            public UIFocus m_UIFocus;
            public Canvas m_Canvas;
        }

        // Dictionary of RectTransforms and their data
        Dictionary<RectTransform, RectTransformData> rectTransformData = new Dictionary<RectTransform, RectTransformData>();

        /// <summary>
        /// The pixel resolution (width x height) of the render texture the Canvas is drawn into.
        /// Determined by the Canvas width and height, not by the Transform Scale or layer size.
        /// Returns <see cref="Vector2Int.zero"/> if the texture has not been generated yet.
        /// </summary>
        public Vector2Int CurrentResolution
        {
            get
            {
                var renderTexture = canvasCamera != null ? canvasCamera.targetTexture : null;
                return renderTexture != null
                    ? new Vector2Int(renderTexture.width, renderTexture.height)
                    : Vector2Int.zero;
            }
        }

        void Awake()
        {
            InitializeCanvas();
        }

        void Start()
        {
            compositionLayer = GetComponent<CompositionLayer>();
            proxyInteractorFactory = new ProxyInteractorFactory();
            canvasHitCalculator = new CanvasHitCalculator(canvas, gameObject);

#if UNITY_EDITOR
            // Hide textures extension component
            texturesExtension = GetComponent<TexturesExtension>();
            texturesExtension.hideFlags = HideFlags.HideInInspector;

            EditorApplication.quitting += OnEditorApplicationQuitting;
#endif
            // Force canvas into WorldSpace render mode and set it's gameObject layer
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasLayerController.CreateAndSetCanvasLayer(canvas);
            canvasLayerCameraController = new CanvasLayerCameraController(canvas.gameObject);

            // Only add components and init synchronizer if the Editor is not playing (like when creating or duplicating an object within the Inspector)
            if (!Application.isPlaying)
            {
                AddComponents();
            }
            else
            {
                // Subscribe to hover events for when application is actually running.
                xrSimpleInteractable.hoverEntered.AddListener(OnHoverEnter);
                xrSimpleInteractable.hoverExited.AddListener(OnHoverExit);
#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
                xrSimpleInteractable.selectFilters.Add(new XRSelectFilterDelegate((_, _) => false));
#endif
            }

#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
            if (xrSimpleInteractable != null)
                xrSimpleInteractable.selectFilters.Add(new XRSelectFilterDelegate((_, _) => false));
#endif

            EnsureCameraWithRenderTexture();

            // Synchronize the canvas and camera.
            canvasAndCameraSynchronizer = new CanvasAndCameraSynchronizer(canvas, canvasCamera);
            SyncTexturesExtensionWithCameraTarget();
        }

        void CreateAndSetCanvas()
        {
            var canvasGameObject = new GameObject("Canvas");
# if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(canvasGameObject, "Create Canvas GameObject");
            Undo.SetTransformParent(canvasGameObject.transform, transform, "Set canvas parent");
#else
            canvasGameObject.transform.SetParent(transform);
#endif
            canvasGameObject.layer = LayerMask.NameToLayer("UI");
            canvas = GetOrAddComponent<Canvas>(canvasGameObject);
        }

        void InitializeCanvas()
        {
            canvas = GetComponentInChildren<Canvas>();

            if (canvas == null)
                CreateAndSetCanvas();

            canvasRectTransform = canvas.GetComponent<RectTransform>();

            GetOrAddComponent<UIHandle>(canvas.gameObject);
            GetOrAddComponent<UIFocus>(canvas.gameObject);

            canvas.renderMode = RenderMode.WorldSpace;
            GetOrAddComponent<CanvasScaler>(canvas.gameObject);
            GetOrAddComponent<GraphicRaycaster>(canvas.gameObject);
#if UNITY_EDITOR
            // Create default panel if no children exist
            if (canvas.transform.childCount == 0)
            {
                GameObject panelGameObject = DefaultControls.CreatePanel(new DefaultControls.Resources { background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd") });
                panelGameObject.transform.SetParent(canvas.transform, false);
                panelGameObject.GetComponent<UnityEngine.UI.Image>().color = Color.white;
                panelGameObject.layer = canvas.gameObject.layer;
                panelGameObject.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(panelGameObject, "Created ui panel");
            }
#endif
        }

        /// <summary>
        /// Updates the texture extension with the new render textures
        /// if there was a change in the canvas rect or scale
        /// </summary>
        void SyncTexturesExtensionWithCameraTarget()
        {
#if UNITY_EDITOR
            if (canvasAndCameraSynchronizer != null)
            {
                if (canvasAndCameraSynchronizer.Sync())
                {
                    texturesExtension.LeftTexture = canvasCamera.targetTexture;
                    texturesExtension.RightTexture = canvasCamera.targetTexture;
                }
            }
            else if (canvas != null && canvasCamera != null)
            {
                canvasAndCameraSynchronizer = new CanvasAndCameraSynchronizer(canvas, canvasCamera);
            }
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            // Keep everything in sync if developer edits the canvas or comp layer sizes.
            SyncTexturesExtensionWithCameraTarget();
            HandleCanvasSize();

            if (compositionLayer != null && canvasCamera != null)
            {
                var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (currentPrefabStage != null && currentPrefabStage == PrefabStageUtility.GetPrefabStage(compositionLayer.gameObject))
                {
                    if (canvasCamera.scene != currentPrefabStage.scene)
                        canvasCamera.scene = currentPrefabStage.scene;
                    canvasCamera.Render();
                }
            }
#endif

            SyncLayerUIScaleWithLayerType();

            // Calculate canvas hits with interactors that have been registered inside OnHoverEnter
            foreach (var interactor in interactors)
            {
                var proxyInteractor = proxyInteractorFactory.GetProxy(interactor);
                if (proxyInteractor == null)
                    continue;

                if (canvasHitCalculator.CalculateCanvasHit(interactor, layerUIScale, out var hitPose))
                {
                    proxyInteractor.transform.position = hitPose.position;
                    proxyInteractor.transform.rotation = hitPose.rotation;
                }
            }
        }

        /// <summary>
        /// Creates a camera to be used for rendering the UI canvas
        /// The created camera is assigned to canvasCamera
        /// </summary>
        /// <seealso cref="canvasCamera"/>
        void CreateCamera()
        {
            // Check in case gameObject has been duplicated.
            if (canvasCamera == null)
            {
                var newCanvasCameraGameObject = new GameObject("CanvasCamera");
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(newCanvasCameraGameObject, "Create canvas camera");
#endif
                canvasCamera = GetOrAddComponent<Camera>(newCanvasCameraGameObject);
            }

            var cameraDistance = -100;
            var cameraGameObject = canvasCamera.gameObject;
            cameraGameObject.transform.parent = canvas.transform;
            cameraGameObject.transform.localScale = Vector3.one;
            cameraGameObject.transform.localPosition = new Vector3(0, 0, cameraDistance);
            cameraGameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            canvasCamera.nearClipPlane = 0f;
            canvasCamera.clearFlags = CameraClearFlags.SolidColor;
            canvasCamera.backgroundColor = new Color(0, 0, 0, 0.001f);

            canvasCamera.stereoTargetEye = StereoTargetEyeMask.None;
            canvasCamera.orthographic = true;
            canvasCamera.gameObject.layer = canvas.gameObject.layer;
        }

        /// <summary>
        /// General initialization such as creating a camera for the ui, adding colliders and interactables to the layer,
        /// and updating the textures off the composition layer to the UI camera
        /// </summary>
        void AddComponents()
        {
            xrSimpleInteractable = GetOrAddComponent<XRSimpleInteractable>(gameObject);
#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
            xrSimpleInteractable.selectFilters.Add(new XRSelectFilterDelegate((_, _) => false));
#endif
            meshCollider = GetOrAddComponent<MeshCollider>(gameObject);
            trackedDeviceGraphicRaycaster = GetOrAddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
            compositionLayerRectTransformData = new RectTransformData
            {
                m_UIHandle = GetOrAddComponent<UIHandle>(gameObject),
                m_UIFocus = GetOrAddComponent<UIFocus>(gameObject)
            };

            SyncLayerUIScaleWithLayerType();

            // Create canvas group
            if (!canvas.TryGetComponent<CanvasGroup>(out canvasGroup))
                canvasGroup = GetOrAddComponent<CanvasGroup>(canvas.gameObject);


            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Adds and removes LayerUIScale components based on their type
        /// </summary>
        /// <remarks>(i.e. a changing from a Cylinder to a Quad layer will remove CylinderUIScale and add QuadUIScale)</remarks>
        void SyncLayerUIScaleWithLayerType()
        {
            if (compositionLayer == null || compositionLayer.LayerData == null)
                return;

            var layerType = compositionLayer.LayerData.GetType();
            var isChangedFromQuad = layerType == typeof(QuadLayerData) && layerUIScale is not QuadUIScale;
            var isChangedFromCylinder = layerType == typeof(CylinderLayerData) && layerUIScale is not CylinderUIScale;

            if (!isChangedFromQuad && !isChangedFromCylinder)
                return;

            if (layerUIScale != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.DestroyObjectImmediate(layerUIScale);
                else
#endif
                    UtilDestroy(layerUIScale);
            }

            if (layerType == typeof(QuadLayerData))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    layerUIScale = Undo.AddComponent<QuadUIScale>(gameObject);
                else
#endif
                    layerUIScale = gameObject.AddComponent<QuadUIScale>();
            }
            else if (layerType == typeof(CylinderLayerData))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    layerUIScale = Undo.AddComponent<CylinderUIScale>(gameObject);
                else
#endif
                    layerUIScale = gameObject.AddComponent<CylinderUIScale>();
            }
        }

        /// <summary>
        /// Ensures the canvas is at least MINIMUM_CANVAS_SIZE on both width and height
        /// </summary>
        void HandleCanvasSize()
        {
            var width = Mathf.Max(canvasRectTransform.rect.width, MINIMUM_CANVAS_SIZE);
            var height = Mathf.Max(canvasRectTransform.rect.height, MINIMUM_CANVAS_SIZE);

            if(width != canvasRectTransform.rect.width || height != canvasRectTransform.rect.height)
                canvasRectTransform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Cleans up creations from AddComponents such as the canvas camera, as well as coliders and render textures
        /// </summary>
        /// <seealso cref="AddComponents"/>
        void DestroyComponents()
        {
            // Only do this for when canvas is no longer a child or component is removed.
            if (gameObject.activeInHierarchy)
            {
                UtilDestroy(layerUIScale);
                UtilDestroy(xrSimpleInteractable);
                UtilDestroy(meshCollider);
                if (compositionLayerRectTransformData != null)
                {
                    UtilDestroy(compositionLayerRectTransformData.m_UIHandle);
                    UtilDestroy(compositionLayerRectTransformData.m_UIFocus);
                }
            }

            // Remove components from UI elements
            if (canvas.gameObject.activeInHierarchy)
            {
                foreach (var rectTransformData in rectTransformData)
                {
                    UtilDestroy(rectTransformData.Value?.m_UIHandle);
                    UtilDestroy(rectTransformData.Value?.m_UIFocus);
                }
            }

            cameraTargetTextureFactory?.ReleaseTargetTexture(canvasCamera);
            texturesExtension.LeftTexture = null;
            texturesExtension.RightTexture = null;
            UtilDestroy(canvasCamera.gameObject);
            UtilDestroy(trackedDeviceGraphicRaycaster);
        }

        /// <summary>
        /// Looks for a component and returns it if found, else will add and return it.
        /// </summary>
        /// <typeparam name="T">The Component to get or add</typeparam>
        /// <returns>the found component or the added component if not found</returns>
        T GetOrAddComponent<T>(GameObject gameObj) where T : Component
        {
            var component = gameObj.GetComponent<T>();
            if (component == null)
#if UNITY_EDITOR
                component = Undo.AddComponent<T>(gameObj);
#else
                component = gameObj.AddComponent<T>();
#endif
            return component;
        }

        void UtilDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;

            UnityObjectUtils.Destroy(obj);
        }

        void OnHoverEnter(HoverEnterEventArgs args)
        {
            if (args.interactorObject is not IXRRayProvider interactor)
                return;

            if (!proxyInteractorFactory.TryCreateOrFind(interactor, canvas.transform.position, out var proxyInteractor))
                return;

            // Set the real interactor and proxy interactor raycast masks.
            int layerBit = canvas.gameObject.layer;

            switch(interactor)
            {
#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
                case NearFarInteractor:
#if UNITY_XR_INTERACTION_TOOLKIT_3_6_0_OR_NEWER
                    var realNearFar = (NearFarInteractor)interactor;
                    var realFarCaster = realNearFar.farInteractionCaster;
                    if (realFarCaster != null)
                        realFarCaster.raycastMask &= ~(1 << layerBit);

                    var proxyNearFar = (NearFarInteractor)proxyInteractor;
                    var proxyFarCaster = proxyNearFar.farInteractionCaster;
                    if (proxyFarCaster != null)
                        proxyFarCaster.raycastMask = 1 << layerBit;
#else
                    var realCaster = ((Component)interactor).GetComponent<CurveInteractionCaster>();
                    if (realCaster != null)
                        realCaster.raycastMask &= ~(1 << layerBit);

                    var proxyCaster = ((Component)proxyInteractor).GetComponent<CurveInteractionCaster>();
                    if (proxyCaster != null)
                        proxyCaster.raycastMask = 1 << layerBit;
#endif
                    break;
#endif
                case XRRayInteractor:
                    // Remove the layer from the real interactor mask
                    (interactor as XRRayInteractor).raycastMask &= ~(1 << layerBit);

                    // Set the proxy interactor mask layer to this layer
                    (proxyInteractor as XRRayInteractor).raycastMask = (1 << layerBit);
                    break;
            }


            // Allow the canvas to receive raycasts
            if (canvasGroup)
                canvasGroup.blocksRaycasts = true;

            SubscribeToProxyUIHoverEvents(interactor);
            interactors.Add(interactor);
        }

        void OnHoverExit(HoverExitEventArgs args)
        {
            if (args.interactorObject is not IXRRayProvider interactor)
                return;

            UnsubscribeFromProxyUIHoverEvents(interactor);
            interactors.Remove(interactor);
        }

        void SubscribeToProxyUIHoverEvents(IXRRayProvider realInteractor)
        {
            var proxyGO = proxyInteractorFactory.GetProxy(realInteractor);
            if (proxyGO == null ||
                !proxyGO.TryGetComponent(out IUIHoverInteractor proxyHover) ||
                realInteractor is not IUIHoverInteractor realHover)
                return;

            proxyHover.uiHoverEntered.AddListener(OnProxyUIHoverEntered);
            proxyHover.uiHoverExited.AddListener(OnProxyUIHoverExited);
            proxyHoverBindings[realInteractor] = new ProxyHoverBinding
            {
                proxyHoverInteractor = proxyHover,
                realHoverInteractor = realHover,
            };
        }

        void UnsubscribeFromProxyUIHoverEvents(IXRRayProvider realInteractor)
        {
            if (!proxyHoverBindings.TryGetValue(realInteractor, out var binding))
                return;

            binding.proxyHoverInteractor.uiHoverEntered.RemoveListener(OnProxyUIHoverEntered);
            binding.proxyHoverInteractor.uiHoverExited.RemoveListener(OnProxyUIHoverExited);

            if (binding.isProxyHoveringUI)
            {
                binding.realHoverInteractor.OnUIHoverExited(
                    new UIHoverEventArgs { interactorObject = binding.realHoverInteractor });
            }

            proxyHoverBindings.Remove(realInteractor);
        }

        void OnProxyUIHoverEntered(UIHoverEventArgs args)
        {
            IXRRayProvider foundInteractor = null;
            ProxyHoverBinding foundBinding = default;

            foreach (var (realInteractor, binding) in proxyHoverBindings)
            {
                if (ReferenceEquals(binding.proxyHoverInteractor, args.interactorObject))
                {
                    foundInteractor = realInteractor;
                    foundBinding = binding;
                    break;
                }
            }

            if (foundInteractor != null)
            {
                foundBinding.isProxyHoveringUI = true;
                proxyHoverBindings[foundInteractor] = foundBinding;
                foundBinding.realHoverInteractor.OnUIHoverEntered(
                    CreateForwardedArgs(foundBinding.realHoverInteractor, args));
            }
        }

        void OnProxyUIHoverExited(UIHoverEventArgs args)
        {
            IXRRayProvider foundInteractor = null;
            ProxyHoverBinding foundBinding = default;

            foreach (var (realInteractor, binding) in proxyHoverBindings)
            {
                if (ReferenceEquals(binding.proxyHoverInteractor, args.interactorObject))
                {
                    foundInteractor = realInteractor;
                    foundBinding = binding;
                    break;
                }
            }

            if (foundInteractor != null)
            {
                foundBinding.isProxyHoveringUI = false;
                proxyHoverBindings[foundInteractor] = foundBinding;
                foundBinding.realHoverInteractor.OnUIHoverExited(
                    CreateForwardedArgs(foundBinding.realHoverInteractor, args));
            }
        }

        static UIHoverEventArgs CreateForwardedArgs(IUIHoverInteractor realHoverInteractor, UIHoverEventArgs proxyArgs)
        {
            return new UIHoverEventArgs
            {
                interactorObject = realHoverInteractor,
                deviceModel = proxyArgs.deviceModel,
                uiObject = proxyArgs.uiObject,
#if UNITY_XR_INTERACTION_TOOLKIT_3_5_0_PRE_1_OR_NEWER
                uiSystem = proxyArgs.uiSystem,
#endif
            };
        }

        static bool IsAlive(object obj)
        {
            return obj switch
            {
                null => false,
                UnityEngine.Object unityObj => unityObj != null,
                _ => true
            };
        }

        void CleanUpAllProxyHoverBindings()
        {
            foreach (var binding in proxyHoverBindings.Values)
            {
                if (IsAlive(binding.proxyHoverInteractor))
                {
                    binding.proxyHoverInteractor.uiHoverEntered.RemoveListener(OnProxyUIHoverEntered);
                    binding.proxyHoverInteractor.uiHoverExited.RemoveListener(OnProxyUIHoverExited);
                }

                if (binding.isProxyHoveringUI && IsAlive(binding.realHoverInteractor))
                {
                    binding.realHoverInteractor.OnUIHoverExited(
                        new UIHoverEventArgs { interactorObject = binding.realHoverInteractor });
                }
            }
            proxyHoverBindings.Clear();
        }

        void OnDestroy()
        {
            CleanUpAllProxyHoverBindings();

            CanvasLayerController.SetCanvasLayerToDefault(canvas);

            if (canvasLayerCameraController != null)
                canvasLayerCameraController.Dispose();
#if UNITY_EDITOR
            EditorApplication.quitting -= OnEditorApplicationQuitting;

            if (xrSimpleInteractable != null)
            {
                xrSimpleInteractable.hoverEntered.RemoveListener(OnHoverEnter);
                xrSimpleInteractable.hoverExited.RemoveListener(OnHoverExit);
            }

            if (texturesExtension != null)
                texturesExtension.hideFlags = HideFlags.None;
            DestroyComponents();
#else
            if (canvas != null)
                CompositionLayerUtils.UserLayers.UnOccupyBlankLayer(canvas.gameObject);
#endif
        }

#if UNITY_EDITOR
        void OnEditorApplicationQuitting()
        {
            // Set layer to default and clean up layers before saving the project during Editor shutdown
            CanvasLayerController.SetCanvasLayerToDefault(canvas);
        }
#endif

        void EnsureCameraWithRenderTexture()
        {
            if (cameraTargetTextureFactory == null)
                cameraTargetTextureFactory = new CameraTargetTextureFactory();

            if (canvasCamera == null)
                CreateCamera();

            if (canvasCamera.targetTexture == null || canvasCamera.activeTexture == null)
            {
                var targetTexture = cameraTargetTextureFactory.CreateTargetTexture(canvasCamera, canvas.GetComponent<RectTransform>().rect);
                texturesExtension.LeftTexture = targetTexture;
                texturesExtension.RightTexture = targetTexture;
            }

            canvasCamera.cullingMask = 1 << canvas.gameObject.layer;
            canvas.worldCamera = canvasCamera;
        }


#if UNITY_EDITOR
        void OnEnable()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
#endif

        void OnDisable()
        {
            CleanUpAllProxyHoverBindings();
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
#endif
        }

#if UNITY_EDITOR
        void OnHierarchyChanged()
        {
            RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>(true);

            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (!rectTransformData.ContainsKey(rectTransform))
                {
                    rectTransformData[rectTransform] = new RectTransformData
                    {
                        m_UIHandle = rectTransform.GetComponent<UIHandle>(),
                        m_UIFocus = rectTransform.GetComponent<UIFocus>(),
                        m_Canvas = rectTransform.GetComponent<Canvas>()
                    };
                }

                var data = rectTransformData[rectTransform];
                if (data.m_UIHandle == null && data.m_Canvas == null)
                    data.m_UIHandle = rectTransform.gameObject.AddComponent<UIHandle>();

                if (data.m_UIFocus == null && data.m_Canvas == null)
                    data.m_UIFocus = rectTransform.gameObject.AddComponent<UIFocus>();
            }
        }

#endif //UNITY_EDITOR
#endif //UNITY_XR_INTERACTION_TOOLKIT
    }
}
