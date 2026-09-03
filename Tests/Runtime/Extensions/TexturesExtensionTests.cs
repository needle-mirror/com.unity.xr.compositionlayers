using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CompositionLayers.Tests;
using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Extensions.Tests
{
    class TexturesExtensionTests : CompositionLayerManagerTestBase
    {
        [Test]
        public async Task TexturesExtensionReportsEyeModification()
        {
            var layer = CreateLayerGameObject();
            layer.ChangeLayerDataType(typeof(QuadLayerData));
            layer.gameObject.SetActive(true);

            layer.AddSuggestedExtensions();

            await new WaitForUpdateCall(this);

            var textureExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(textureExt);

            textureExt.TargetEye = TexturesExtension.TargetEyeEnum.Individual;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.TargetEye = TexturesExtension.TargetEyeEnum.Individual;
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);
        }

        [Test]
        public async Task TexturesExtensionReportsEyeTextureModification()
        {
            var layer = CreateLayerGameObject();
            layer.ChangeLayerDataType(typeof(QuadLayerData));
            layer.gameObject.SetActive(true);

            layer.AddSuggestedExtensions();
            await new WaitForUpdateCall(this);

            var textureExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(textureExt);

            textureExt.LeftTexture = Texture2D.whiteTexture;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.LeftTexture = Texture2D.whiteTexture;
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.LeftTexture = Texture2D.blackTexture;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.LeftTexture = null;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);
        }

        [Test]
        public async Task TexturesExtensionReportsMipMapModeModification()
        {
            var layer = CreateLayerGameObject();
            layer.ChangeLayerDataType(typeof(QuadLayerData));
            layer.gameObject.SetActive(true);

            layer.AddSuggestedExtensions();
            await new WaitForUpdateCall(this);

            var textureExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(textureExt);
            Assert.AreEqual(TexturesExtension.MipMapModeEnum.None, textureExt.MipMapMode);

            textureExt.MipMapMode = TexturesExtension.MipMapModeEnum.CopyFromSource;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            // Setting the same value again should not report a modification.
            textureExt.MipMapMode = TexturesExtension.MipMapModeEnum.CopyFromSource;
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.MipMapMode = TexturesExtension.MipMapModeEnum.AutoGenerate;
            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);
            Assert.IsFalse(hasModifiedLayers);
        }
    }
}
