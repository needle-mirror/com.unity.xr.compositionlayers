using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.UIInteraction
{
    partial class CanvasLayerCameraController : IDisposable
    {
        // The cameras cached that we manage
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        static readonly List<Camera> s_ConfiguredCameras = new();

        public GameObject canvasToManage { get; set; }

        internal CanvasLayerCameraController(GameObject canvasToManage)
        {
            this.canvasToManage = canvasToManage;

            Camera.onPreRender += OnCameraPreRender;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

            RemoveObjectFromAllCameras(canvasToManage);
        }

        void OnCameraPreRender(Camera cam)
        {
            AddCameraToDictionary(cam);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            AddCameraToDictionary(cam);
        }

        void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            // Clear cache when scenes unload to avoid stale references
            s_ConfiguredCameras.Clear();
        }

        void AddCameraToDictionary(Camera cam)
        {
            if (!s_ConfiguredCameras.Contains(cam))
            {
                if (canvasToManage != null)
                    ClearLayerFromCamera(cam, 1 << canvasToManage.layer);
                s_ConfiguredCameras.Add(cam);
            }
        }

        static void ClearLayerFromCamera(Camera cam, int culledLayersMask)
        {
            // Do not remove the layer if it is the only one left for this camera.
            // These cameras isolate specific layers and reducing the culling mask to 0
            // leaves the camera with no render target, breaking its functionality.
            if ((cam.cullingMask & ~culledLayersMask) != 0)
            {
                cam.cullingMask &= ~culledLayersMask;
            }
        }

        public static void RemoveObjectFromAllCameras(GameObject objectToCull)
        {
            // The copied list will contain the same object references as original list.
            var listCopy = new List<Camera>(s_ConfiguredCameras);
            foreach (var cam in listCopy)
            {
                if (cam == null)
                {
                    s_ConfiguredCameras.Remove(cam);
                    continue;
                }
                else
                    ClearLayerFromCamera(cam, 1 << objectToCull.layer);
            }
        }

        public void Dispose()
        {
            Camera.onPreRender -= OnCameraPreRender;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
}
