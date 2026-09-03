using Unity.XR.CompositionLayers.Provider;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.Services
{
    /// <summary>
    /// For managing active PlatformProvider.
    /// </summary>
    public static partial class PlatformManager
    {
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static readonly PlatformProvider s_DefaultPlatformProvider = new DefaultPlatformProvider();

#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        static PlatformProvider s_ActivePlatformProvider;
#if UNITY_EDITOR
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        static bool s_IsSupportedHDROnPreview;
#endif

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

        /// <summary>
        /// Check for supporting HDR
        /// </summary>
        internal static bool IsSupportedHDR
        {
            get
            {
#if UNITY_EDITOR
                return s_IsSupportedHDROnPreview;
#else
                return s_ActivePlatformProvider != null && s_ActivePlatformProvider.IsSupportedHDR;
#endif
            }
            set
            {
#if UNITY_EDITOR
                s_IsSupportedHDROnPreview = value;
#endif
            }
        }
    }
}
