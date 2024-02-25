using UnityEngine;

namespace Unity.XR.CompositionLayers.Emulation
{
    /// <summary>
    /// For keeping extra attributes for Camera.
    /// </summary>
    internal struct EmulatedCameraData
    {
        public Camera Camera;
        public bool IsSceneView;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="camera">Target Camera</param>
        public EmulatedCameraData(Camera camera)
        {
            this.Camera = camera;
            this.IsSceneView = camera.cameraType == CameraType.SceneView;
        }
    }
}
