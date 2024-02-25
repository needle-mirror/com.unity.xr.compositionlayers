#if UNITY_XR_INTERACTION_TOOLKIT
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Handles creation and deletion of "ProxyInteractors", proxy controllers used for interaction with a composition layer's Canvas
    /// </summary>
    internal class ProxyInteractorFactory
    {
        // Name to give instantiated interactors
        const string ProxyInteractorName = "ProxyInteractor";

        // Dictionary of normal interactors to their proxy canvas interactors
        private static Dictionary<IXRHoverInteractor, GameObject> interactorsToProxys = new Dictionary<IXRHoverInteractor, GameObject>();

        /// <summary>
        /// Gets a proxy interactor based on passed proxy
        /// </summary>
        /// <param name="interactor"></param>
        /// <returns>A proxy interactor based on passed proxy</returns>
        public GameObject GetProxy(XRRayInteractor interactor)
        {
            interactorsToProxys.TryGetValue(interactor, out var proxy);
            return proxy;
        }

        /// <summary>
        /// Creates or finds a proxy interactor from a supplied interactor
        /// </summary>
        /// <param name="interactor">The interactor</param>
        /// <param name="position">The position to put the proxy interactor</param>
        /// <param name="proxyInteractor">The created or found interactor</param>
        /// <returns>Whether or not it has been created or found</returns>
        public bool TryCreateOrFind(XRRayInteractor interactor, Vector3 position, out XRRayInteractor proxyInteractor)
        {
            proxyInteractor = null;

            if (interactor.name == ProxyInteractorName)
                return false;

            if (!interactorsToProxys.ContainsKey(interactor) && interactor.xrController is ActionBasedController)
            {
                var proxyGameObject = CreateControllerAndRayInteractor(interactor.xrController as ActionBasedController, position);
                interactorsToProxys.Add(interactor, proxyGameObject);
            }

            proxyInteractor = interactorsToProxys[interactor].GetComponent<XRRayInteractor>();
            return true;
        }

        /// <summary>
        /// Helper for creating Proxy Interactors
        /// </summary>
        /// <param name="actionBasedController">Interactor to base the proxy from</param>
        /// <param name="position">Position to create the proxy controller</param>
        /// <returns>The created Proxy Interactor GameObject</returns>
        private GameObject CreateControllerAndRayInteractor(ActionBasedController actionBasedController, Vector3 position)
        {
            var interactorGameObject = new GameObject(ProxyInteractorName);
            interactorGameObject.transform.position = position;
            interactorGameObject.hideFlags = HideFlags.HideInHierarchy;

            var xrController = interactorGameObject.AddComponent<ActionBasedController>();
            xrController.selectAction = actionBasedController.selectAction;
            xrController.activateAction = actionBasedController.activateAction;
            xrController.uiPressAction = actionBasedController.uiPressAction;

            var xrRayInteractor = interactorGameObject.AddComponent<XRRayInteractor>();
            xrRayInteractor.rayOriginTransform = interactorGameObject.transform;
            xrRayInteractor.maxRaycastDistance = 500;
            xrRayInteractor.raycastMask = 0;

            return interactorGameObject;
        }
    }
}
#endif
