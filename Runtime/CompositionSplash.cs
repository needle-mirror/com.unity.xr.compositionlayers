using System;
using System.Collections;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

using AsyncReturnType = UnityEngine.Awaitable;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// The CompositionLayer package splash screen class.
    /// </summary>
    public class CompositionSplash : MonoBehaviour
    {
        /// <summary>
        /// The CompositionLayer component that will be used to display the splash screen.
        /// </summary>
        [Header("References")]
        [Tooltip("The CompositionLayer component that will be used to display the splash screen.")]
        public CompositionLayer compositionLayer;

        /// <summary>
        /// The TexturesExtension component attached to the CompositionLayer to control the splash screen texture.
        /// </summary>
        [Tooltip("The TexturesExtension component attached to the CompositionLayer to control the splash screen texture.")]
        public TexturesExtension splashTexture;

        /// <summary>
        /// The ColorScaleBiasExtension component attached to the CompositionLayer to control the splash screen alpha.
        /// </summary>
        [Tooltip("The ColorScaleBiasExtension component attached to the CompositionLayer to control the splash screen alpha.")]
        public ColorScaleBiasExtension colorScaleBias;

        bool m_MainCameraRendered = false;

        CompositionLayersRuntimeSettings m_Settings;

        Camera m_MainCamera;

        AsyncReturnType splashScreenAwaitable;

        bool passthroughInitialized = false;

        void Awake()
        {
            if (!Validate()) return;

            m_Settings = RuntimeSettingsLookup.Current;
            if (splashScreenAwaitable != null && !splashScreenAwaitable.IsCompleted)
            {
                splashScreenAwaitable.Cancel();
                splashScreenAwaitable = null;
            }

            SetupCamera(false);
            SetupLayer();

            splashScreenAwaitable = DisplaySplashscreens();
        }

        void Update()
        {
            // We can't spawn the layer in awake, as it is ran before OpenXRLayerProvider.Started, where the layer is registered.
            if (CompositionLayerManager.PassthroughLayerType != null && !passthroughInitialized)
            {
                bool passthroughSetup = TrySetupPassthrough();
                SetupCamera(passthroughSetup);
                passthroughInitialized = true;
            }

            Camera camera = GetMainCamera();

            var lerpPosition = Vector3.Lerp(
                compositionLayer.transform.position,
                GetTargetPosition(m_Settings.LockToHorizon),
                Time.deltaTime * m_Settings.FollowSpeed);
            var lerpRotation = Quaternion.Lerp(
                compositionLayer.transform.rotation,
                GetTargetRotation(m_Settings.LockToHorizon),
                Time.deltaTime * m_Settings.FollowSpeed);

            compositionLayer.transform.position = TransformPointToNearestPointOnSphere(
                lerpPosition, camera.transform.position, m_Settings.FollowDistance);
            compositionLayer.transform.rotation = lerpRotation;
        }

        void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += (_, camera) => OnCameraPostRender(camera);
        }

        void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= (_, camera) => OnCameraPostRender(camera);
        }

        void OnCameraPostRender(Camera cam)
        {
            if (m_MainCameraRendered || cam != GetMainCamera() || compositionLayer == null) return;

            m_MainCameraRendered = true;

            compositionLayer.transform.position = TransformPointToNearestPointOnSphere(
                GetTargetPosition(m_Settings.LockToHorizon),
                cam.transform.position,
                m_Settings.FollowDistance);
            compositionLayer.transform.rotation = GetTargetRotation(m_Settings.LockToHorizon);
        }

        void SetupLayer()
        {
            // Set layer type
            switch (m_Settings.LayerType)
            {
                case CompositionLayersRuntimeSettings.Layer.Quad:
                    compositionLayer.ChangeLayerDataType(m_Settings.QuadLayerData);
                    break;
                case CompositionLayersRuntimeSettings.Layer.Cylinder:
                    compositionLayer.ChangeLayerDataType(m_Settings.CylinderLayerData);
                    break;
            }
        }

        bool TrySetupPassthrough()
        {
            if (m_Settings == null)
                return false;

            if (m_Settings.BackgroundType != CompositionLayersRuntimeSettings.SplashBackgroundType.Passthrough)
                return false;

            Type passthroughType = CompositionLayerManager.PassthroughLayerType;
            if (passthroughType == null)
            {
                Debug.LogWarning("Passthrough background style is selected, but no passthrough layer is available.");
                return false;
            }

            Layers.LayerDataDescriptor passthroughDescriptor = CompositionLayerUtils.GetLayerDescriptor(passthroughType);
            if (passthroughDescriptor == null)
            {
                Debug.LogWarning("Passthrough Layer Data Descriptor is not found.");
                return false;
            }

            Layers.LayerData passthroughLayerData = CompositionLayerUtils.CreateLayerData(passthroughType);
            if (passthroughLayerData == null)
            {
                Debug.LogWarning("Passthrough Layer Data is not found.");
                return false;
            }

            CompositionLayer passthroughLayer = new GameObject("Passthrough Layer").AddComponent<CompositionLayer>();

            passthroughLayer.ChangeLayerDataType(passthroughLayerData);
            foreach (var extension in passthroughDescriptor.SuggestedExtensions)
            {
                if (extension.IsSubclassOf(typeof(MonoBehaviour)))
                    passthroughLayer.gameObject.AddComponent(extension);
            }

            passthroughLayer.TryChangeLayerOrder(passthroughLayer.Order, CompositionLayerManager.GetFirstUnusedLayer(false));
            return true;
        }

        Vector3 TransformPointToNearestPointOnSphere(Vector3 point, Vector3 center, float radius)
        {
            Vector3 direction = point - center;
            direction.Normalize();
            return center + direction * radius;
        }

        async AsyncReturnType DisplaySplashscreens()
        {
            // Set splash texture
            if (m_Settings.SplashImage != null)
                splashTexture.LeftTexture = m_Settings.SplashImage;

            // Hide splash screen
            SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 0.0f));

            // Fade in
            if (m_Settings.FadeInDuration <= 0)
            {
                SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 1.0f));
            }
            else
            {
                float timer = 0.0f;
                while (timer < m_Settings.FadeInDuration)
                {
                    timer += Time.deltaTime;
                    SetColorScale(ColorScaleBiasLerp(timer, m_Settings.FadeInDuration));
                    await WaitForEndOfFrame();
                }
            }

            // Wait for splash duration
            await WaitForSeconds(m_Settings.SplashDuration);

            // Fade out
            if (m_Settings.FadeOutDuration <= 0)
            {
                SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 0.0f));
            }
            else
            {
                float timer = 0.0f;
                while (timer < m_Settings.FadeOutDuration)
                {
                    timer += Time.deltaTime;
                    SetColorScale(ColorScaleBiasLerp(
                        m_Settings.FadeOutDuration - timer, m_Settings.FadeOutDuration));
                    await WaitForEndOfFrame();
                }
            }

            // Load scene if possible
            int sceneToLoad = GetSceneToLoad();
            if (sceneToLoad != -1) SceneManager.LoadScene(sceneToLoad);
        }

        void SetColorScale(Vector4 scale)
        {
            colorScaleBias.Scale = scale;
        }

        void SetupCamera(bool passthrough)
        {
            Camera camera = GetMainCamera();

            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = passthrough ? Color.clear : m_Settings.BackgroundColor;
        }

        Vector4 ColorScaleBiasLerp(float timer, float duration)
        {
            return new Vector4(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.0f, 1.0f, timer / duration));
        }

        bool Validate()
        {
            if (compositionLayer == null)
            {
                Debug.LogError("CompositionLayer is not set");
                return false;
            }

            if (splashTexture == null)
            {
                Debug.LogError("SplashTexture is not set");
                return false;
            }

            if (colorScaleBias == null)
            {
                Debug.LogError("ColorScaleBias is not set");
                return false;
            }

            if (GetMainCamera() == null)
            {
                Debug.LogError("Main camera is not found");
                return false;
            }

            return true;
        }

        Vector3 GetTargetPosition(bool lockToHorizon = false)
        {
            Camera camera = GetMainCamera();
            Vector3 target = camera.transform.position + camera.transform.forward * m_Settings.FollowDistance;
            return lockToHorizon ? new Vector3(target.x, camera.transform.position.y, target.z) : target;
        }

        Quaternion GetTargetRotation(bool lockToHorizon = false)
        {
            Camera camera = GetMainCamera();
            return Quaternion.Euler(lockToHorizon ? 0 : camera.transform.eulerAngles.x, camera.transform.eulerAngles.y, 0.0f);
        }

        int GetSceneToLoad()
        {
            for (int i = SceneManager.GetActiveScene().buildIndex + 1; i < SceneManager.sceneCountInBuildSettings; i++)
                if (SceneUtility.GetScenePathByBuildIndex(i) != null)
                    return i;

            return -1;
        }

        Camera GetMainCamera()
        {
            if (m_MainCamera == null)
                m_MainCamera = Camera.main;

            return m_MainCamera;
        }

        async AsyncReturnType WaitForEndOfFrame()
        {
            await Awaitable.EndOfFrameAsync();
        }

        async AsyncReturnType WaitForSeconds(float seconds)
        {
            await Awaitable.WaitForSecondsAsync(seconds);
        }
    }
}
