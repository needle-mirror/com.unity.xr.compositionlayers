#if UNITY_XR_INTERACTION_TOOLKIT
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_XR_INTERACTION_TOOLKIT_3_0_0
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#endif

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Helper class for transforming raycast hit's to the Composition Layer to the attached Canvas
    /// </summary>
    internal class CanvasHitCalculator
    {
        // The canvas to raycast
        private Canvas canvas;

        // The GameObject of the attached Composition Layer
        private GameObject compositionLayerGameObject;

        // the RectTransform of the attached Canvas
        private RectTransform canvasRectTransform;

        /// <summary>
        /// Initializer
        /// </summary>
        /// <param name="canvas">The canvas to raycast to</param>
        /// <param name="compositionLayerGameObject">The GameObject of the composition layer</param>
        public CanvasHitCalculator(Canvas canvas, GameObject compositionLayerGameObject)
        {
            this.canvas = canvas;
            this.compositionLayerGameObject = compositionLayerGameObject;
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Calculates location on canvas where the raycast hit
        /// </summary>
        /// <param name="interactor">Current interactor</param>
        /// <param name="layerUIScale">The active LayerUIScale component for coordinate mapping</param>
        /// <param name="hitPose">Calculated hit pose</param>
        /// <returns>whether or not the raycast hit the Composition Layer</returns>
        public bool CalculateCanvasHit(IXRRayProvider interactor, LayerUIScale layerUIScale, out Pose hitPose)
        {
            hitPose = Pose.identity;

            if (layerUIScale == null ||
                layerUIScale.WidthScale == 0f ||
                layerUIScale.HeightScale == 0f)
                return false;

            if (interactor.rayEndTransform)
            {
                var canvasHitPosition = RecalculateHitToCanvas(interactor.rayEndPoint, layerUIScale);
                var pointerDistance = -0.1f;

                var pointerToCanvasLocalPosition = canvas.transform.TransformPoint(canvasHitPosition);
                var finalPosition = pointerToCanvasLocalPosition + (canvas.transform.forward * pointerDistance);
                var finalRotation = Quaternion.LookRotation(canvas.transform.forward);
                hitPose = new Pose(finalPosition, finalRotation);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Calculates location on canvas where the raycast hit from a RaycastHit.
        /// Calls proper function depending if layer is a Quad or a Cylinder as the calculations vary.
        /// </summary>
        /// <param name="raycastHit">The hit position against the composition layer</param>
        /// <param name="layerUIScale">The active LayerUIScale component for coordinate mapping</param>
        /// <returns>The location on the canvas where the ray should hit</returns>
        private Vector3 RecalculateHitToCanvas(Vector3 raycastHit, LayerUIScale layerUIScale)
        {
            var hasCompositionLayer = compositionLayerGameObject.TryGetComponent<CompositionLayer>(out var compositionLayer);
            if (!hasCompositionLayer || compositionLayer.LayerData == null)
                return Vector3.zero;

            return compositionLayer.LayerData switch
            {
                QuadLayerData quadLayerData => RecalculateHitToCanvas(raycastHit, quadLayerData, layerUIScale),
                CylinderLayerData cylinderLayerData => RecalculateHitToCanvas(raycastHit, cylinderLayerData, layerUIScale),
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// Calculates location on canvas where the raycast hit from a RaycastHit
        /// </summary>
        /// <param name="raycastHit">The hit position against the composition layer</param>
        /// <param name="quadLayerData">The hit layer</param>
        /// <param name="layerUIScale">The active LayerUIScale component for coordinate mapping</param>
        /// <returns>The location on the canvas where the ray should hit</returns>
        private Vector3 RecalculateHitToCanvas(Vector3 raycastHit, QuadLayerData quadLayerData, LayerUIScale layerUIScale)
        {
            var quadSize = quadLayerData.GetScaledSize(compositionLayerGameObject.transform.lossyScale);
            var scale = quadLayerData.ApplyTransformScale ? compositionLayerGameObject.transform.lossyScale : Vector3.one;
            var localHit = compositionLayerGameObject.transform.InverseTransformPoint(raycastHit);

            var localX = -localHit.x / (quadSize.x * 0.5f) / layerUIScale.WidthScale;
            var localY = -localHit.y / (quadSize.y * 0.5f) / layerUIScale.HeightScale;
            var localHitPosition = new Vector2(localX, localY);

            var canvasX = localHitPosition.x * canvasRectTransform.rect.x * scale.x;
            var canvasY = localHitPosition.y * canvasRectTransform.rect.y * scale.y;
            var canvasHitPosition = new Vector3(canvasX, canvasY, 0);

            return canvasHitPosition;
        }

        /// <summary>
        /// Calculates location on canvas where the raycast hit from a raycast hit position.
        /// </summary>
        /// <param name="raycastHit">The hit position against the composition layer</param>
        /// <param name="cylinderLayerData">The hit layer</param>
        /// <param name="layerUIScale">The active LayerUIScale component for coordinate mapping</param>
        /// <returns>The location on the canvas where the ray should hit</returns>
        private Vector3 RecalculateHitToCanvas(Vector3 raycastHit, CylinderLayerData cylinderLayerData, LayerUIScale layerUIScale)
        {
            var scale = compositionLayerGameObject.transform.lossyScale;
            var layerHitPoint = compositionLayerGameObject.transform.worldToLocalMatrix.MultiplyPoint3x4(raycastHit);
            var scaledHitPoint = new Vector3(layerHitPoint.x * scale.x, 0, layerHitPoint.z);

            // angle between hit point and cylinder center
            var angle = -Vector3.SignedAngle(scaledHitPoint.normalized, Vector3.forward, Vector3.up);

            // Invert the y height to be used as a scalar
            var yHeightInverted = 1.0f / (cylinderLayerData.GetHeight() / 2.0f);

            // Scale the hit point to a relative size of 1x1
            var centralAngleHalf = cylinderLayerData.CentralAngleInDegrees / 2.0f;
            var canvasPos = new Vector2(
                Remap(angle, -centralAngleHalf, centralAngleHalf, -1, 1),
                layerHitPoint.y * yHeightInverted);

            // Scale the hit point on the composition layer to the world space canvas
            var hitX = canvasPos.x * canvasRectTransform.sizeDelta.x / 2.0f / layerUIScale.WidthScale;
            var hitY = canvasPos.y * canvasRectTransform.sizeDelta.y / 2.0f / layerUIScale.HeightScale;
            var hitPosition = new Vector2(hitX, hitY);
            var canvasHitPosition = new Vector3(hitPosition.x * (1 / scale.x), hitPosition.y, 0);

            return canvasHitPosition;
        }

        /// <summary>
        /// Remaps a value from one range to another
        /// </summary>
        /// <param name="value">The value to remap</param>
        /// <param name="oldMin">The min of the first range</param>
        /// <param name="oldMax">the max of the first range</param>
        /// <param name="newMin">the min of the second range</param>
        /// <param name="newMax">the max of the second range</param>
        /// <returns>The new mapped value on the second range</returns>
        private float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
        }
    }
}
#endif
