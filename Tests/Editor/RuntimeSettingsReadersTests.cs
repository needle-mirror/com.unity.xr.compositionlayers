using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using UnityEditor;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class RuntimeSettingsReadersTests : RuntimeSettingsTestBase
    {
        CompositionLayersRuntimeSettings m_Override;

        [SetUp]
        public void CreateOverride()
        {
            m_Override = ScriptableObject.CreateInstance<CompositionLayersRuntimeSettings>();
            RuntimeSettingsLookup.Override = m_Override;
        }

        [TearDown]
        public void DestroyOverride()
        {
            RuntimeSettingsLookup.Override = null;
            Object.DestroyImmediate(m_Override);
        }

        [TestCase(true, CompositionLayersRuntimeSettings.SplashBackgroundType.Passthrough, true)]
        [TestCase(true, CompositionLayersRuntimeSettings.SplashBackgroundType.SolidColor, false)]
        [TestCase(false, CompositionLayersRuntimeSettings.SplashBackgroundType.Passthrough, false)]
        [TestCase(false, CompositionLayersRuntimeSettings.SplashBackgroundType.SolidColor, false)]
        public void PassthroughSplashScreenEnabled_ReadsLookup(
            bool splashEnabled, CompositionLayersRuntimeSettings.SplashBackgroundType backgroundType, bool expected)
        {
            var serialized = new SerializedObject(m_Override);
            serialized.FindProperty("m_EnableSplashScreen").boolValue = splashEnabled;
            serialized.FindProperty("m_BackgroundType").enumValueIndex = (int)backgroundType;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(expected, CompositionLayersRuntimeSettings.PassthroughSplashScreenEnabled);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EmulationGate_ReadsLookup(bool emulationInStandalone)
        {
            var serialized = new SerializedObject(m_Override);
            serialized.FindProperty("m_EmulationInStandalone").boolValue = emulationInStandalone;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(emulationInStandalone, EmulatedCompositionLayerUtils.IsEmulationInStandaloneEnabled);
        }
    }
}
