using System;

namespace Unity.XR.CompositionLayers.Rendering.Editor
{
#pragma warning disable CS0618
    /// <summary>
    /// Provides no functionality. Retained for compatibility.
    /// </summary>
    /// <remarks>
    /// This type is deprecated. It formerly added the composition layer emulation shaders to the
    /// Always Included Shaders list. Those shaders are now included in every build automatically,
    /// so they no longer need registration in graphics settings. Remove calls to this type; no
    /// replacement is required. For more information, refer to
    /// [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#graphics-settings-loader).
    /// </remarks>
    [Obsolete("GraphicsSettingsLoader is deprecated in XR Composition Layers 7.0.0-pre.1 and will be removed in a future release. The emulation shaders are now included in every build automatically, so they no longer need to be added to Always Included Shaders. This type performs no work.")]
    public static class GraphicsSettingsLoader
    {
    }
#pragma warning restore CS0618
}
