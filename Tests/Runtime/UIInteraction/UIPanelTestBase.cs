#if UNITY_XR_INTERACTION_TOOLKIT
using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.UIInteraction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.UIInteraction
{
    /// <summary>
    /// Builds a UI Panel layer the way a script does at runtime and drives XRI hover against it.
    /// </summary>
    abstract class UIPanelTestBase : CompositionLayerManagerTestBase
    {
        readonly List<GameObject> m_CreatedGameObjects = new();
        readonly HashSet<XRBaseInteractor> m_InteractorsBeforeTest = new();
        readonly HashSet<XRInteractionManager> m_InteractionManagersBeforeTest = new();

        protected override void Setup()
        {
            base.Setup();
            m_InteractorsBeforeTest.UnionWith(FindInteractors());
            m_InteractionManagersBeforeTest.UnionWith(FindAll<XRInteractionManager>());
        }

        protected override void Teardown()
        {
            DestroyCreatedObjects();
            base.Teardown();
        }

        protected InteractableUIMirror CreatePanel()
        {
            var panel = CreateGameObject("UIPanelLayer");
            panel.SetActive(false);
            panel.AddComponent<CompositionLayer>().ChangeLayerDataType<QuadLayerData>();

            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(panel.transform, false);

            // In the Editor, CompositionLayer adds the mirror itself when a Canvas is parented to a Quad layer.
            if (!panel.TryGetComponent<InteractableUIMirror>(out var mirror))
                mirror = panel.AddComponent<InteractableUIMirror>();

            panel.SetActive(true);
            Assert.AreEqual(1, panel.GetComponents<InteractableUIMirror>().Length);
            return mirror;
        }

        /// <summary>
        /// Creates a ray interactor and moves it and the panel onto a manager owned by the test, so both are
        /// guaranteed to register with the same manager.
        /// </summary>
        protected XRRayInteractor CreateInteractor(InteractableUIMirror panel)
        {
            var interactionManager = CreateGameObject("TestInteractionManager").AddComponent<XRInteractionManager>();
            GetInteractable(panel).interactionManager = interactionManager;

            var interactorGameObject = CreateGameObject("TestRayInteractor");
            interactorGameObject.SetActive(false);
            var interactor = interactorGameObject.AddComponent<XRRayInteractor>();
            interactor.interactionManager = interactionManager;
            interactorGameObject.SetActive(true);
            return interactor;
        }

        /// <summary>
        /// Returns the proxy the mirror created for <paramref name="interactor"/> on hover enter.
        /// </summary>
        protected XRRayInteractor FindProxyInteractor(XRRayInteractor interactor)
        {
            XRRayInteractor proxy = null;
            foreach (var candidate in FindInteractors())
            {
                if (candidate == interactor || m_InteractorsBeforeTest.Contains(candidate))
                    continue;

                Assert.IsNull(proxy, "Expected a single proxy interactor.");
                proxy = candidate as XRRayInteractor;
            }

            Assert.IsNotNull(proxy, "The mirror did not create a proxy interactor.");
            return proxy;
        }

        protected static void ForceHover(XRRayInteractor interactor, InteractableUIMirror panel)
        {
            var interactable = GetInteractable(panel);
            interactable.interactionManager.HoverEnter((IXRHoverInteractor)interactor, interactable);
        }

        protected static void ForceHoverExit(XRRayInteractor interactor, InteractableUIMirror panel)
        {
            var interactable = GetInteractable(panel);
            interactable.interactionManager.HoverExit((IXRHoverInteractor)interactor, interactable);
        }

        protected static bool CanvasBlocksRaycasts(InteractableUIMirror panel)
        {
            return panel.GetComponentInChildren<CanvasGroup>(true).blocksRaycasts;
        }

        protected static XRSimpleInteractable GetInteractable(InteractableUIMirror panel)
        {
            return panel.GetComponent<XRSimpleInteractable>();
        }

        GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            m_CreatedGameObjects.Add(gameObject);
            return gameObject;
        }

        static XRBaseInteractor[] FindInteractors()
        {
            return FindAll<XRBaseInteractor>();
        }

        static T[] FindAll<T>() where T : UnityObject
        {
#if UNITY_6000_4_OR_NEWER
            return UnityObject.FindObjectsByType<T>(FindObjectsInactive.Include);
#else
            return UnityObject.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
        }

        // The base sweep only covers composition layers, not the XRI objects or the mirror's hidden proxy interactors.
        void DestroyCreatedObjects()
        {
            // Any interactor that did not exist before the test is either the test's own or a proxy the mirror made.
            foreach (var interactor in FindInteractors())
            {
                if (interactor != null && !m_InteractorsBeforeTest.Contains(interactor))
                    UnityObject.DestroyImmediate(interactor.gameObject);
            }

            m_InteractorsBeforeTest.Clear();

            // The panel's interactable is enabled before the test's manager exists, so XRI auto-creates a default
            // manager in the DontDestroyOnLoad scene. Left behind, it becomes the default for later tests in other packages.
            foreach (var interactionManager in FindAll<XRInteractionManager>())
            {
                if (interactionManager != null && !m_InteractionManagersBeforeTest.Contains(interactionManager))
                    UnityObject.DestroyImmediate(interactionManager.gameObject);
            }

            m_InteractionManagersBeforeTest.Clear();

            // A panel created before entering Play mode can come back as a new object when exiting Play mode restores
            // the edit mode scene. The base sweep removes it along with every other layer.
            foreach (var gameObject in m_CreatedGameObjects)
            {
                if (gameObject != null)
                    UnityObject.DestroyImmediate(gameObject);
            }

            m_CreatedGameObjects.Clear();
        }
    }
}
#endif
