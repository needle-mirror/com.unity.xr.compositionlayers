using Unity.XR.CompositionLayers.Extensions;
using UnityEditor;
using UnityEngine;
using System;

namespace Unity.XR.CompositionLayers.Layers
{
    /// <summary>
    /// Projection rig layer type for full screen rendering of a texture composition layer.
    /// </summary>
    [Serializable]
    [CompositionLayerData(
        Provider = "Unity",
        Name = "Projection Layer Eye Rig",
        IconPath = CompositionLayerConstants.IconPath,
        InspectorIcon = "LayerEyeColor",
        ListViewIcon = "LayerEye",
        Description = "Projection Layer Eye Rig",
        SuggestedExtensionTypes = new Type[] { }
    )]
    public class ProjectionLayerRigData : LayerData
    {
        internal const string k_MissingCamerasMessage =
            "This Projection Eye Rig is missing its Left Camera or Right Camera, so it cannot render. "
            + "Delete it and create a new one from GameObject > XR > Composition Layers > Projection Eye Rig.";

        [SerializeField, HideInInspector]
        Camera m_LeftCamera;

        [SerializeField, HideInInspector]
        Camera m_RightCamera;

        // False only for rigs saved before the camera references existed, which adopt their first two children.
        [SerializeField, HideInInspector]
        bool m_CamerasAssigned;

        internal Camera LeftCamera => m_LeftCamera;

        internal Camera RightCamera => m_RightCamera;

        internal void SetCameras(Camera leftCamera, Camera rightCamera)
        {
            m_LeftCamera = leftCamera;
            m_RightCamera = rightCamera;
            m_CamerasAssigned = true;
        }

        /// <summary>
        /// Gets the rig's eye cameras that exist under <paramref name="rigTransform"/>. A rig saved before the
        /// references existed adopts its first two children.
        /// </summary>
        /// <returns><see langword="true"/> if both cameras exist.</returns>
        internal bool TryGetCameras(Transform rigTransform, out Camera leftCamera, out Camera rightCamera)
        {
            if (!m_CamerasAssigned)
                AdoptChildCameras(rigTransform);

            leftCamera = GetCameraUnder(m_LeftCamera, rigTransform);
            rightCamera = GetCameraUnder(m_RightCamera, rigTransform);
            return leftCamera != null && rightCamera != null;
        }

        // Copied layer data can still reference another rig's cameras.
        static Camera GetCameraUnder(Camera camera, Transform rigTransform)
        {
            if (camera == null || camera.transform == rigTransform || !camera.transform.IsChildOf(rigTransform))
                return null;

            return camera;
        }

        void AdoptChildCameras(Transform rigTransform)
        {
            if (rigTransform.childCount < 2)
                return;

            var leftCamera = rigTransform.GetChild(0).GetComponent<Camera>();
            var rightCamera = rigTransform.GetChild(1).GetComponent<Camera>();
            if (leftCamera == null || rightCamera == null)
                return;

            SetCameras(leftCamera, rightCamera);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Undoes what creating the rig did to <paramref name="layer"/>, recorded in the current undo group.
        /// </summary>
        internal void RemoveFromLayer(CompositionLayer layer)
        {
            var undoName = Undo.GetCurrentGroupName();
            TryGetCameras(layer.transform, out var leftCamera, out var rightCamera);
            DestroyCamera(leftCamera);
            DestroyCamera(rightCamera);

            var texturesExtension = layer.GetComponent<TexturesExtension>();
            if (texturesExtension != null)
            {
                // RecordObject does not track hideFlags.
                Undo.RegisterCompleteObjectUndo(texturesExtension, undoName);
                texturesExtension.LeftTexture = null;
                texturesExtension.RightTexture = null;
                texturesExtension.hideFlags = HideFlags.None;
            }

            Undo.RecordObject(layer.gameObject, undoName);
            layer.gameObject.layer = 0;
        }

        static void DestroyCamera(Camera camera)
        {
            if (camera != null)
                Undo.DestroyObjectImmediate(camera.gameObject);
        }
#endif
    }
}
