using Unity.XR.CompositionLayers.Services;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Contains constant values useful for working with composition layers.
    /// </summary>
    static class CompositionLayerConstants
    {
        /// <summary>
        /// Name the Uber shader declares. It draws emulated layers.
        /// </summary>
        internal const string UberShaderName = "Unlit/XRCompositionLayers/Uber";

        /// <summary>
        /// Name the Color Scale Bias shader declares. It applies the Color Scale Bias extension in emulation.
        /// </summary>
        internal const string ColorScaleBiasShaderName = "Hidden/XRCompositionLayers/ColorScaleBias";

        /// <summary>
        /// Name the Blit Copy HDR shader declares. It is used by the mirror view.
        /// </summary>
        internal const string BlitCopyHDRShaderName = "Unlit/XRCompositionLayers/BlitCopyHDR";

        internal const string IconPath = "Packages/com.unity.xr.compositionlayers/Editor/Icons/";

        /// <summary>
        /// Name of the in GameObject created to provide Monobehaviour callbacks for the <see cref="Unity.XR.CompositionLayers.Services.CompositionLayerManager"/>
        /// </summary>
        internal const string SceneManagerName = "~Compositon Manager";

        /// <summary>
        /// Name of the in default scene composition layer GameObject. <see cref="CompositionLayerManager.DefaultSceneCompositionLayer"/>
        /// </summary>
        internal const string DefaultSceneLayerName = "Default Scene Layer";
    }
}
