using System.Collections;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class CompositionSplash : MonoBehaviour
{
    [Header("References")]
    /// <summary>
    /// The CompositionLayer component that will be used to display the splash screen.
    /// </summary>
    public CompositionLayer compositionLayer;

    /// <summary>
    /// The TexturesExtension component attached to the CompositionLayer to control the splash screen texture.
    /// </summary>
    public TexturesExtension splashTexture;

    /// <summary>
    /// The ColorScaleBiasExtension component attached to the CompositionLayer to control the splash screen alpha.
    /// </summary>
    public ColorScaleBiasExtension colorScaleBias;

    private bool mainCameraRendered = false;

    private CompositionLayersRuntimeSettings pref;

    private Camera mainCamera;
    
    void Awake()
    {
        if(!Validate()) return;

        pref = CompositionLayersRuntimeSettings.Instance;

        SetupCamera();
        SetupLayer();
        StartCoroutine(DisplaySplashScreens());
    }

    void Update()
    {
        Camera camera = GetMainCamera();

        Vector3 lerpPosition = Vector3.Lerp(compositionLayer.transform.position, GetTargetPosition(pref.LockToHorizon), Time.deltaTime * pref.FollowSpeed);
        Quaternion lerpRotation = Quaternion.Lerp(compositionLayer.transform.rotation, GetTargetRotation(pref.LockToHorizon), Time.deltaTime * pref.FollowSpeed);

        compositionLayer.transform.position = TransformPointToNearestPointOnSphere(lerpPosition, camera.transform.position, pref.FollowDistance);
        compositionLayer.transform.rotation = lerpRotation;
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += (_, camera) => OnCameraPostRender(camera);
        Camera.onPostRender += OnCameraPostRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= (_, camera) => OnCameraPostRender(camera);
        Camera.onPostRender -= OnCameraPostRender;
    }

    private void OnCameraPostRender(Camera cam)
    {
        if(mainCameraRendered || cam != GetMainCamera() || compositionLayer == null) return;

        mainCameraRendered = true;
        
        compositionLayer.transform.position = TransformPointToNearestPointOnSphere(GetTargetPosition(pref.LockToHorizon), cam.transform.position, pref.FollowDistance);
        compositionLayer.transform.rotation = GetTargetRotation(pref.LockToHorizon);
    }

    private void SetupLayer()
    {
        // Set layer type
        switch(pref.LayerType)
        {
            case CompositionLayersRuntimeSettings.Layer.Quad:
                compositionLayer.ChangeLayerDataType(pref.QuadLayerData);
                break;
            case CompositionLayersRuntimeSettings.Layer.Cylinder:
                compositionLayer.ChangeLayerDataType(pref.CylinderLayerData);
                break;
        }
    }

    Vector3 TransformPointToNearestPointOnSphere(Vector3 point, Vector3 center, float radius)
    {
        Vector3 direction = point - center;
        direction.Normalize();
        return center + direction * radius;
    }

    IEnumerator DisplaySplashScreens()
    {
        // Set splash texture
        if(pref.SplashImage != null)
            splashTexture.LeftTexture = pref.SplashImage;
        
        // Hide splash screen
        SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 0.0f));

        // Fade in
        if(pref.FadeInDuration <= 0)
        {
            SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 1.0f));
        }
        else
        {
            float timer = 0.0f;
            while (timer < pref.FadeInDuration)
            {
                timer += Time.deltaTime;
                SetColorScale(ColorScaleBiasLerp(timer, pref.FadeInDuration));
                yield return null;
            }
        }

        // Wait for splash duration
        yield return new WaitForSeconds(pref.SplashDuration);

        // Fade out
        if(pref.FadeOutDuration <= 0)
        {
            SetColorScale(new Vector4(colorScaleBias.Scale.x, colorScaleBias.Scale.y, colorScaleBias.Scale.z, 0.0f));
        }
        else
        {
            float timer = 0.0f;
            while (timer < pref.FadeOutDuration)
            {
                timer += Time.deltaTime;
                SetColorScale(ColorScaleBiasLerp(pref.FadeOutDuration - timer, pref.FadeOutDuration));
                yield return null;
            }
        }

        // Load scene if possible
        int sceneToLoad = GetSceneToLoad();
        if(sceneToLoad != -1) SceneManager.LoadScene(sceneToLoad);
    }

    void SetColorScale(Vector4 scale)
    {
        colorScaleBias.Scale = scale;
    }

    private void SetupCamera()
    {
        Camera camera = GetMainCamera();

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = pref.BackgroundColor;
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
        
        if(GetMainCamera() == null)
        {
            Debug.LogError("Main camera is not found");
            return false;
        }

        return true;
    }

    private Vector3 GetTargetPosition(bool lockToHorizon = false)
    {
        Camera camera = GetMainCamera();
        Vector3 target = camera.transform.position + camera.transform.forward * pref.FollowDistance;
        return lockToHorizon ? new Vector3(target.x, camera.transform.position.y, target.z) : target;
    }

    private Quaternion GetTargetRotation(bool lockToHorizon = false)
    {
        Camera camera = GetMainCamera();
        return Quaternion.Euler(lockToHorizon ? 0 : camera.transform.eulerAngles.x, camera.transform.eulerAngles.y, 0.0f);
    }

    int GetSceneToLoad()
    {
        for(int i = SceneManager.GetActiveScene().buildIndex + 1; i < SceneManager.sceneCountInBuildSettings; i++)
            if(SceneUtility.GetScenePathByBuildIndex(i) != null)
                return i;

        return -1;
    }

    Camera GetMainCamera()
    {
        if(mainCamera == null)
            mainCamera = Camera.main;

        return mainCamera;
    }
}
