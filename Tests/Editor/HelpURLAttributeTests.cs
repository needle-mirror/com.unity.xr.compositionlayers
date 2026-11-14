using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    [TestFixture]
    class HelpURLAttributeTests
    {
        const string k_PackageName = "com.unity.xr.compositionlayers";

        [Test]
        public void FallbackVersionMatchesPackageMajorMinorVersion()
        {
            var packageInfo = PackageInfo.FindForPackageName(k_PackageName);
            Assert.That(packageInfo, Is.Not.Null);
            Assert.That(packageInfo.version, Is.Not.Null.And.Not.Empty);

            var majorMinorVersion = GetMajorMinor(packageInfo.version);
            Assert.AreEqual(majorMinorVersion, CompositionLayersHelpURLAttribute.k_FallbackVersion,
                $"{nameof(CompositionLayersHelpURLAttribute)}.k_FallbackVersion should match the package's major.minor version ({majorMinorVersion}).");
        }

        static string GetMajorMinor(string version)
        {
            // Return major.minor from the string, for example "7.0.0-pre.1" returns "7.0".
            var secondDotIndex = version.IndexOf('.', version.IndexOf('.') + 1);
            Assert.That(secondDotIndex, Is.GreaterThan(0));
            return version.Substring(0, secondDotIndex);
        }
    }
}
