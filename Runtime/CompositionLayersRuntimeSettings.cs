using System;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Project-wide Composition Layers settings, edited in Project Settings > XR Plug-in Management >
    /// Composition Layers.
    /// </summary>
    public class CompositionLayersRuntimeSettings : ScriptableObject
    {
        [SerializeField, HideInInspector]
        Shader m_UberShader;

        [SerializeField, HideInInspector]
        Shader m_ColorScaleBiasShader;

        [SerializeField, HideInInspector]
        Shader m_BlitCopyHDRShader;

        [SerializeField]
        [Tooltip("Draws composition layers through emulation in standalone players without an XR display. " +
            "Ignored when an XR display is running.")]
        bool m_EmulationInStandalone = false;

        [SerializeField]
        [Tooltip("Enable or disable the splash screen.")]
        bool m_EnableSplashScreen = false;

        [SerializeField]
        [Tooltip("Image to display on the splash screen.")]
        Texture m_SplashImage = null;

        [SerializeField]
        [Tooltip("Background type of the splash screen.")]
        SplashBackgroundType m_BackgroundType = SplashBackgroundType.SolidColor;

        [SerializeField]
        [Tooltip("Background color of the splash screen.")]
        Color m_BackgroundColor = new Color(0.1372549f, 0.1215686f, 0.1254902f, 1.0f);

        [SerializeField]
        [Tooltip("Duration of the splash screen.")]
        float m_SplashDuration = 3f;

        [SerializeField, Min(0.0f)]
        [Tooltip("Duration of the fade in.")]
        float m_FadeInDuration = 2.0f;

        [SerializeField, Min(0.0f)]
        [Tooltip("Duration of the fade out.")]
        float m_FadeOutDuration = 1.0f;

        [SerializeField, Min(0.0f)]
        [Tooltip("Speed at which the layer lerps to the follow position.")]
        float m_FollowSpeed = 2.0f;

        [SerializeField]
        [Tooltip("Distance from the camera to the splash screen.")]
        float m_FollowDistance = 2.0f;

        [SerializeField]
        [Tooltip("Lock the splash screen to the horizon.")]
        bool m_LockToHorizon = true;

        [SerializeField]
        [Tooltip("Type of layer the splash screen is drawn on.")]
        Layer m_LayerType = Layer.Quad;

        [SerializeField]
        QuadLayerData m_QuadLayerData = new QuadLayerData();

        [SerializeField]
        CylinderLayerData m_CylinderLayerData = new CylinderLayerData();

        /// <summary>
        /// Defines the types of layers that can be used.
        /// </summary>
        public enum Layer
        {
            /// <summary>
            /// Quad Layer
            /// </summary>
            Quad,

            /// <summary>
            /// Cylinder Layer
            /// </summary>
            Cylinder
        }

        /// <summary>
        /// Defines the types of backgrounds that can be used.
        /// </summary>
        public enum SplashBackgroundType
        {
            /// <summary>
            /// Solid Color Background, uses the background color.
            /// </summary>
            SolidColor,

            /// <summary>
            /// Passthrough Background, uses the passthrough layer.
            /// </summary>
            Passthrough
        }

        /// <summary>
        /// The project's settings, or the defaults when the project has no settings asset. Never creates an asset.
        /// </summary>
        [Obsolete("Instance is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future " +
            "release. Reference the settings asset from a serialized field instead.")]
        public static CompositionLayersRuntimeSettings Instance => RuntimeSettingsLookup.Current;

        /// <summary>
        /// Returns true if the Splash Screen is enabled and the Background Type is set to Passthrough.
        /// </summary>
        public static bool PassthroughSplashScreenEnabled
        {
            get
            {
                var settings = RuntimeSettingsLookup.Current;
                return settings.EnableSplashScreen && settings.BackgroundType == SplashBackgroundType.Passthrough;
            }
        }

        /// <summary>
        /// Deprecated in XR Composition Layers 7.0.0-pre.1. Whether standalone players without an XR display draw
        /// composition layers through emulation. Has no effect when an XR display is running. For more information,
        /// refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#emulation-in-standalone).
        /// </summary>
        [Obsolete("EmulationInStandalone is deprecated in XR Composition Layers 7.0.0-pre.1 and will be removed in a " +
            "future release. It only applies to standalone players without an XR display. To hide layers in those " +
            "builds, disable them instead.")]
        public bool EmulationInStandalone => m_EmulationInStandalone;

        /// <summary>
        /// Gets a value indicating whether the splash screen is enabled.
        /// </summary>
        public bool EnableSplashScreen => m_EnableSplashScreen;

        /// <summary>
        /// Gets the image to display on the splash screen.
        /// </summary>
        public Texture SplashImage => m_SplashImage;

        /// <summary>
        /// Gets the background type of the splash screen.
        /// Solid color will use the background color, passthrough will show the passthrough layer.
        /// </summary>
        public SplashBackgroundType BackgroundType => m_BackgroundType;

        /// <summary>
        /// Gets the background color of the splash screen.
        /// </summary>
        public Color BackgroundColor => m_BackgroundColor;

        /// <summary>
        /// Gets the duration of the splash screen.
        /// </summary>
        public float SplashDuration => m_SplashDuration;

        /// <summary>
        /// Gets the duration of the fade-in effect.
        /// </summary>
        public float FadeInDuration => m_FadeInDuration;

        /// <summary>
        /// Gets the duration of the fade-out effect.
        /// </summary>
        public float FadeOutDuration => m_FadeOutDuration;

        /// <summary>
        /// Gets the speed at which the layer moves towards the follow position.
        /// </summary>
        public float FollowSpeed => m_FollowSpeed;

        /// <summary>
        /// Gets the distance from the camera to the splash screen.
        /// </summary>
        public float FollowDistance => m_FollowDistance;

        /// <summary>
        /// Gets a value indicating whether the splash screen is locked to the horizon.
        /// </summary>
        public bool LockToHorizon => m_LockToHorizon;

        /// <summary>
        /// Gets the type of layer used for the splash screen.
        /// </summary>
        public Layer LayerType => m_LayerType;

        /// <summary>
        /// Gets the data for the quad layer.
        /// </summary>
        public QuadLayerData QuadLayerData => m_QuadLayerData;

        /// <summary>
        /// Gets the data for the cylinder layer.
        /// </summary>
        public CylinderLayerData CylinderLayerData => m_CylinderLayerData;

        /// <summary>
        /// The Uber shader, which draws emulated layers. Assigned by setup.
        /// </summary>
        internal Shader UberShader
        {
            get => m_UberShader;
            set => m_UberShader = value;
        }

        /// <summary>
        /// The Color Scale Bias shader, which applies the Color Scale Bias extension in emulation. Assigned by setup.
        /// </summary>
        internal Shader ColorScaleBiasShader
        {
            get => m_ColorScaleBiasShader;
            set => m_ColorScaleBiasShader = value;
        }

        /// <summary>
        /// The Blit Copy HDR shader, used by the mirror view. Assigned by setup.
        /// </summary>
        internal Shader BlitCopyHDRShader
        {
            get => m_BlitCopyHDRShader;
            set => m_BlitCopyHDRShader = value;
        }

        internal bool IsEmulationInStandaloneEnabled => m_EmulationInStandalone;

#if !UNITY_EDITOR
        // The player only loads the asset that the build added to Preloaded Assets.
        void OnEnable()
        {
            RuntimeSettingsLookup.Register(this);
        }

        void OnDisable()
        {
            RuntimeSettingsLookup.Unregister(this);
        }
#endif
    }
}
