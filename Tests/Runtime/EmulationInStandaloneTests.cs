using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;

namespace Unity.XR.CompositionLayers.Tests
{
    // The deprecated EmulationInStandalone setting only applies to standalone players with no XR display running.
    // Anywhere an XR display runs, layers fall back to emulation whatever the setting.
    class EmulationInStandaloneTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PlayerEmulation_WithXRDisplay_IgnoresSetting(bool emulationInStandalone)
        {
            Assert.IsTrue(EmulatedCompositionLayerUtils.IsEmulationAllowedInPlayer(
                isStandalonePlayer: true, isXRDisplayActive: true, emulationInStandalone));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerEmulation_WithoutXRDisplay_FollowsSetting(bool emulationInStandalone)
        {
            Assert.AreEqual(emulationInStandalone, EmulatedCompositionLayerUtils.IsEmulationAllowedInPlayer(
                isStandalonePlayer: true, isXRDisplayActive: false, emulationInStandalone));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void NotStandalonePlayer_IgnoresSetting(bool isXRDisplayActive, bool emulationInStandalone)
        {
            Assert.IsTrue(EmulatedCompositionLayerUtils.IsEmulationAllowedInPlayer(
                isStandalonePlayer: false, isXRDisplayActive, emulationInStandalone));
        }
    }
}
