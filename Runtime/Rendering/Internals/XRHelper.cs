using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Unity.XR.CompositionLayers.Rendering.Internals
{
    internal static class XRHelper
    {
        public static XRLoader GetActiveLoader()
        {
            return XRGeneralSettings.Instance?.Manager?.activeLoader;
        }

        public static XRDisplaySubsystem GetActiveDisplaySubsystem()
        {
            var displaySubsystem = GetActiveLoader()?.GetLoadedSubsystem<XRDisplaySubsystem>();
            return displaySubsystem != null && displaySubsystem.running ? displaySubsystem : null;
        }
    }
}
