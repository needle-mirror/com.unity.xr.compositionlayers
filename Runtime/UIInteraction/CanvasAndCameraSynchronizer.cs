using UnityEngine;

namespace Unity.XR.CompositionLayers.UIInteraction
{
    /// <summary>
    /// Helper class for adjusting the camera bounds and render texture to match the canvas
    /// </summary>
    internal class CanvasAndCameraSynchronizer
    {
        // RectTransform of the Canvas
        private RectTransform canvasRectTransform;

        // Camera used to Render the Canvas
        private Camera canvasCamera;

        // Used to generate RenderTextures for the Camera
        private CameraTargetTextureFactory cameraTargetTextureFactory;

        // Cached scale to check for changes of the Canvas scale
        private Vector3 cachedCanvasLossyScale;

        // Cached Rect to check for changes of the Canvas Rect
        private Rect cachedCanvasRect;

        /// <summary>
        /// Initializer
        /// </summary>
        /// <param name="canvas">Canvas which should be rendered</param>
        /// <param name="canvasCamera">Camera to render the canvas</param>
        public CanvasAndCameraSynchronizer(Canvas canvas, Camera canvasCamera)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
            this.canvasCamera = canvasCamera;
            cameraTargetTextureFactory = new CameraTargetTextureFactory();
        }

        /// <summary>
        /// Updates the camera framing when the Canvas rect or scale changes, and recreates the render texture
        /// only when the rect changes, since the texture size depends on the rect alone.
        /// </summary>
        /// <returns>True if the render texture was recreated, false otherwise</returns>
        public bool Sync()
        {
            var rect = canvasRectTransform.rect;
            var lossyScale = canvasRectTransform.lossyScale;
            var rectChanged = rect != cachedCanvasRect;
            var scaleChanged = lossyScale != cachedCanvasLossyScale;
            if (!rectChanged && !scaleChanged)
            {
                return false;
            }

            SyncCameraViewWithCanvasSize();
            cachedCanvasLossyScale = lossyScale;

            if (!rectChanged)
            {
                return false;
            }

            SyncRenderTextureWithCanvasSize();
            cachedCanvasRect = rect;
            return true;
        }

        /// <summary>
        /// Syncs the Camera bounds with the size of the Canvas
        /// </summary>
        private void SyncCameraViewWithCanvasSize()
        {
            float canvasWidth = canvasRectTransform.rect.width * canvasRectTransform.lossyScale.x;
            float canvasHeight = canvasRectTransform.rect.height * canvasRectTransform.lossyScale.y;
            canvasCamera.orthographicSize = canvasHeight * 0.5f;
            canvasCamera.aspect = canvasWidth / canvasHeight;
        }

        /// <summary>
        /// Regenerates the render texture for the canvas camera with the current canvas rect
        /// </summary>
        private void SyncRenderTextureWithCanvasSize()
        {
            cameraTargetTextureFactory.ReplaceTargetTexture(canvasCamera, canvasRectTransform.rect);
        }
    }
}
