using Unity.XR.CompositionLayers.UIInteraction;
using UnityEngine;

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Custom gizmo creator attached to every UI element under a Composition UI Layer
    /// Handles position, rotation, and scale calculations to draw Gizmos
    /// </summary>
    /// /// <seealso cref="UIHandleEditor"/>
    [ExecuteInEditMode, RequireComponent(typeof(RectTransform))]
    public class UIHandle : MonoBehaviour
    {
        /// <summary>
        /// Reference to the parent Composition Layer's Transform
        /// </summary>
        public Transform CompositionLayerTransform { get => m_CompositionLayerTransform; }
        private Transform m_CompositionLayerTransform;
        
        /// <summary>
        /// Reference to the Canvas this element is attached to
        /// </summary>
        public Canvas Canvas { get => m_Canvas; }
        private Canvas m_Canvas;

        /// <summary>
        /// Reference to this element's RectTransform
        /// </summary>
        public RectTransform RectTransform { get => m_RectTransform; }
        private RectTransform m_RectTransform;

        /// <inheritdoc cref="MonoBehaviour"/>
        void OnEnable()
        {
            m_CompositionLayerTransform = GetComponentInParent<CompositionLayer>()?.transform;
            m_Canvas = GetComponentInParent<Canvas>();
            m_RectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Calculates the position of the RectTransform in the layer's local space
        /// </summary>
        /// <returns>The position of the RectTransform in the layer's local space</returns>
        public Vector3 GetHandlePosition()
        {
            if(!m_CompositionLayerTransform || !m_Canvas) return Vector3.zero;

            Vector3 m_CanvasScale = GetComponentInParent<LayerUIScale>().GetUIScale();
            // Vector3 m_CanvasScale = Vector3.one;

            Vector3 localPosition = RectTransform.localPosition;
            Vector3 scaledLocalPosition = Vector3.Scale(localPosition, m_CanvasScale);
            Vector3 scaledLocalPositionToCompositionSpace = m_CompositionLayerTransform.TransformPoint(scaledLocalPosition);

            return scaledLocalPositionToCompositionSpace;
        }

        /// <summary>
        /// Calculates the rotation of the RectTransform in the layer's local space
        /// </summary>
        /// <returns>The RectTransform's rotation</returns>
        public Quaternion GetHandleRotation()
        {
            return RectTransform.rotation;
        }

        /// <summary>
        /// Calculates the scale of the RectTransform in the layer's local space
        /// </summary>
        /// <returns>The RectTransform's scale</returns>
        public Vector3 GetHandleScale()
        {
            return RectTransform.localScale;
        }

        /// <summary>
        /// Sets the position of the RectTransform
        /// </summary>
        /// <remarks>
        /// Typically called with a return value from Handles.PositionHandle, Handles.RotationHandle, or Handles.ScaleHandle
        /// </remarks>
        /// <param name="worldSpace">The location in World Space</param>
        public void SetRectPosition(Vector3 worldSpace)
        {
            if(!m_CompositionLayerTransform || !m_Canvas) return;

            Vector3 inverseCanvasScale = GetComponentInParent<LayerUIScale>().GetInverseUIScale();

            Vector3 worldPositionToCompositionSpace = m_CompositionLayerTransform.InverseTransformPoint(worldSpace);
            Vector3 scaledCompositionSpace = Vector3.Scale(worldPositionToCompositionSpace, inverseCanvasScale);

            RectTransform.localPosition = new Vector3(scaledCompositionSpace.x, scaledCompositionSpace.y, 0);
        }
    }
}
