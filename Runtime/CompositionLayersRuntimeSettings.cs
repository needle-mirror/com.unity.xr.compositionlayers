using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Settings class for composition layer emulation in standalone.
    /// </summary>
    [ScriptableSettingsPath("Assets/CompositionLayers/UserSettings")]
    public class CompositionLayersRuntimeSettings : ScriptableSettings<CompositionLayersRuntimeSettings>
    {
        public enum Layer { Quad, Cylinder }

        [SerializeField]
        [Tooltip("Enable or disable emulation of composition layers in standalone builds when no XR provider is active or no headset connected.")]
        bool m_EmulationInStandalone = false;

        public bool EmulationInStandalone => m_EmulationInStandalone;

        [Header("Composition Layer Splash Settings")]
        [SerializeField]
        [Tooltip("Enable or disable the splash screen.")] 
        bool m_EnableSplashScreen = false;

        public bool EnableSplashScreen => m_EnableSplashScreen;

        [Header("Style Settings")]
        [SerializeField]
        [Tooltip("Image to display on the splash screen.")]
        Texture m_SplashImage = null;

        public Texture SplashImage => m_SplashImage;
        
        [SerializeField]
        [Tooltip("Background color of the splash screen.")]
        Color m_BackgroundColor = new Color(0.1372549f, 0.1215686f, 0.1254902f, 1.0f);

        public Color BackgroundColor => m_BackgroundColor; 
        
        [Header("Duration Settings")]
        [SerializeField]
        [Tooltip("Duration of the splash screen.")]
        float m_SplashDuration = 3f;

        public float SplashDuration => m_SplashDuration;

        [SerializeField, Min(0.0f)] 
        [Tooltip("Duration of the fade in.")]
        float m_FadeInDuration = 2.0f;

        public float FadeInDuration => m_FadeInDuration;

        [SerializeField, Min(0.0f)]
        [Tooltip("Duration of the fade out.")]
        float m_FadeOutDuration = 1.0f;

        public float FadeOutDuration => m_FadeOutDuration;

        [Header("Follow Settings")]
        [SerializeField, Min(0.0f)]
        [Tooltip("Speed at which the layer lerps to the follow position.")]
        float m_FollowSpeed = 2.0f;

        public float FollowSpeed => m_FollowSpeed;

        [SerializeField]
        [Tooltip("Distance from the camera to the splash screen.")]
        float m_FollowDistance = 2.0f;

        public float FollowDistance => m_FollowDistance;

        [SerializeField]
        [Tooltip("Lock the splash screen to the horizon.")]
        bool m_LockToHorizon = true;

        public bool LockToHorizon => m_LockToHorizon;

        [Header("Layer Settings")]
        [SerializeField]
        Layer m_LayerType = Layer.Quad;
        public Layer LayerType => m_LayerType;

        [SerializeField]
        QuadLayerData m_QuadLayerData = new QuadLayerData();

        public QuadLayerData QuadLayerData => m_QuadLayerData;

        [SerializeField]
        CylinderLayerData m_CylinderLayerData = new CylinderLayerData();
        
        public CylinderLayerData CylinderLayerData => m_CylinderLayerData;
    }
}
