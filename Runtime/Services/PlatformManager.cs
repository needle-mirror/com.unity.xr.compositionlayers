using Unity.XR.CompositionLayers.Provider;

namespace Unity.XR.CompositionLayers.Services
{
    /// <summary>
    /// For managing active PlatformProvider.
    /// </summary>
    public static class PlatformManager
    {
        static readonly PlatformProvider s_DefaultPlatformProvider = new DefaultPlatformProvider();

        static PlatformProvider s_ActivePlatformProvider;

        /// <summary>
        /// Active PlatformProvider.
        /// This value is set from PlatformSelector on Editor.
        /// This value is set from XRLoader on Player.
        /// </summary>
        public static PlatformProvider ActivePlatformProvider
        {
            get => s_ActivePlatformProvider != null ? s_ActivePlatformProvider : s_DefaultPlatformProvider;
            set => s_ActivePlatformProvider = value;
        }
    }
}
