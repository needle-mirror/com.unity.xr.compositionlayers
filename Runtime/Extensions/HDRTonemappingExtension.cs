using System;
using UnityEngine;
using Unity.XR.CompositionLayers.Provider;

namespace Unity.XR.CompositionLayers.Extensions
{
    /// <summary>
    /// Subclass of <see cref="CompositionLayerExtension" /> that describes the HDR encoding of the source texture
    /// of the <see cref="CompositionLayer"/> instance on the same game object: its color gamut, the luminance of
    /// paper white, and its maximum luminance.
    ///
    /// Providers that support HDR output use these values to convert the layer's content for the display.
    /// Editor emulation also uses them when the Game view outputs HDR. Support for this component is up to the
    /// instance of <see cref="ILayerProvider" /> currently assigned to the
    /// <see cref="Unity.XR.CompositionLayers.Services.CompositionLayerManager" />; the OpenXR provider ignores it.
    ///
    /// If this extension is not added to a layer game object, the layer's source texture is treated as not
    /// HDR-encoded.
    /// </summary>
    // The PlayStation®VR2 provider (`com.unity.xr.psvr2`) reads this component at runtime for its HDR layers and adds
    // it when converting layers, so check that package before changing or removing this API.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Composition Layers/Extensions/HDR Tonemapping")]
    [CompositionLayersHelpURL(typeof(HDRTonemappingExtension))]
    public class HDRTonemappingExtension : CompositionLayerExtension
    {
        [SerializeField]
        [Tooltip("The value used to color gamut for the source texture.")]
        ColorGamut m_ColorGamut = ColorGamut.sRGB;

        [SerializeField]
        [Tooltip("The value used to describe nits for paper white for the source texture.")]
        int m_NitsForPaperWhite = 160;

        [SerializeField]
        [Tooltip("The value used to describe max display nits for the source texture.")]
        int m_MaxDisplayNits = 160;

        /// <summary>
        /// Gets the target of the extension, which is a layer.
        /// </summary>
        public override ExtensionTarget Target { get { return ExtensionTarget.Layer; } }

        /// <summary>
        /// Retrieves a pointer to the native structure.
        /// </summary>
        /// <returns>A null pointer as this extension does not use a native structure.</returns>
        public override unsafe void* GetNativeStructPtr() { return null; }

        /// <summary>
        /// The value used to color gamut for the source texture.
        /// </summary>
        public ColorGamut ColorGamut
        {
            get => m_ColorGamut;
            set => m_ColorGamut = UpdateValue(m_ColorGamut, value);
        }

        /// <summary>
        /// The value used to describe nits for paper white for the source texture.
        /// </summary>
        public int NitsForPaperWhite
        {
            get => m_NitsForPaperWhite;
            set => m_NitsForPaperWhite = UpdateValue(m_NitsForPaperWhite, value);
        }

        /// <summary>
        /// The value used to describe max display nits for the source texture.
        /// </summary>
        public int MaxDisplayNits
        {
            get => m_MaxDisplayNits;
            set => m_MaxDisplayNits = UpdateValue(m_MaxDisplayNits, value);
        }
    }
}
