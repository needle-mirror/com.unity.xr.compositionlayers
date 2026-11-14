using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Tests;

namespace Unity.XR.CompositionLayers.Extensions.Tests
{
    /// <summary>
    /// Tests various eye texture handling options on <see cref="TexturesExtension"/>: the default target eye,
    /// per-eye texture assignment, and the Individual / Left / Right target-eye modes. Left and Right
    /// only exist when the OpenXR 1.18+ define is present, so those cases are compiled conditionally.
    /// </summary>
    class TexturesExtensionEyeTests : CompositionLayerManagerTestBase
    {
        [Test]
        public void DefaultTargetEye_IsBoth()
        {
            var ext = CreateTrackedLayer().gameObject.AddComponent<TexturesExtension>();

            Assert.AreEqual(TexturesExtension.TargetEyeEnum.Both, ext.TargetEye);
        }

        [Test]
        public async Task PerEyeTextureAssignment_ReportsModified_AndSetsTextureAdded([Values] bool leftEye)
        {
            var layer = CreateActiveLayer<QuadLayerData>(addSuggestedExtensions: true);
            await NextManagerUpdate();
            var ext = GetExtension<TexturesExtension>(layer);
            Assert.IsNotNull(ext);
            await NextManagerUpdate(); // settle

            ext.TextureAdded = false;
            if (leftEye)
                ext.LeftTexture = Texture2D.whiteTexture;
            else
                ext.RightTexture = Texture2D.whiteTexture;

            await NextManagerUpdate();
            Assert.IsTrue(hasModifiedLayers);
            Assert.IsTrue(ext.TextureAdded, "Assigning a texture to a previously-null eye should set TextureAdded.");
        }

        [Test]
        public async Task ChangingTargetEyeToIndividual_ReportsModified()
        {
            var layer = CreateActiveLayer<QuadLayerData>(addSuggestedExtensions: true);
            await NextManagerUpdate();
            var ext = GetExtension<TexturesExtension>(layer);
            Assert.IsNotNull(ext);
            await NextManagerUpdate();

            ext.TargetEye = TexturesExtension.TargetEyeEnum.Individual;
            await NextManagerUpdate();
            Assert.IsTrue(hasModifiedLayers);

            // Setting the same value does not report a modification.
            await NextManagerUpdate();
            ext.TargetEye = TexturesExtension.TargetEyeEnum.Individual;
            await NextManagerUpdate();
            Assert.IsFalse(hasModifiedLayers);
        }

#if XR_OPENXR_7_0_0_PRE_1_OR_NEWER
        [Test]
        public async Task ChangingTargetEyeToLeftThenRight_ReportsModified()
        {
            var layer = CreateActiveLayer<QuadLayerData>(addSuggestedExtensions: true);
            await NextManagerUpdate();
            var ext = GetExtension<TexturesExtension>(layer);
            Assert.IsNotNull(ext);
            await NextManagerUpdate();

            ext.TargetEye = TexturesExtension.TargetEyeEnum.Left;
            await NextManagerUpdate();
            Assert.IsTrue(hasModifiedLayers);
            await NextManagerUpdate();

            ext.TargetEye = TexturesExtension.TargetEyeEnum.Right;
            await NextManagerUpdate();
            Assert.IsTrue(hasModifiedLayers);
        }
#endif
    }
}
