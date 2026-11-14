using System;
using Unity.XR.CompositionLayers.Provider;

namespace Unity.XR.CompositionLayers.Tests
{
    // Stands in for a platform so tests can drive capability-based decisions. Installed on
    // PlatformManager.ActivePlatformProvider for the duration of a test.
    class TestPlatformProvider : PlatformProvider
    {
        public override Type XRLoaderType => null;

        public override Type LayerProviderType => null;
    }
}
