using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    class CompositionLayerManagerTests : CompositionLayerManagerTestBase
    {
        [Test]
        public async Task ChangingTextureExtensionReportsModifiedLayer()
        {
            var layer = CreateLayerGameObject();
            layer.gameObject.SetActive(true);
            layer.AddSuggestedExtensions();
            await new WaitForUpdateCall(this);

            var textureExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNull(textureExt);
            await new WaitForUpdateCall(this);

            var layerId = typeof(CylinderLayerData).FullName;
            var layerData = CompositionLayerUtils.CreateLayerData(layerId);
            layer.ChangeLayerDataType(layerData);
            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsTrue(hasActiveLayers);
            Assert.IsFalse(hasRemovedLayers);
            Assert.IsTrue(hasModifiedLayers);
            await new WaitForUpdateCall(this);

            layer.AddSuggestedExtensions();
            textureExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(textureExt);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsTrue(hasActiveLayers);
            Assert.IsFalse(hasRemovedLayers);
            Assert.IsFalse(hasModifiedLayers);

            textureExt.TargetEye = TexturesExtension.TargetEyeEnum.Individual;

            await new WaitForUpdateCall(this);

            Assert.IsTrue(hasModifiedLayers);

            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasModifiedLayers);

            textureExt.TargetEye = TexturesExtension.TargetEyeEnum.Individual;

            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasModifiedLayers);

            UnityObject.DestroyImmediate(layer.gameObject);
        }

        [Test]
        public async Task CompositionLayerManagerRemovedLayerNotInAddedList()
        {
            // Create Layer to start manager
            var layerHold = CreateLayerGameObject();
            var layerHoldGo = layerHold.gameObject;
            layerHoldGo.SetActive(true);
            await new WaitForUpdateCall(this);

            var layerDestroy = CreateLayerGameObject();
            var layerDestroyGo = layerDestroy.gameObject;
            layerDestroyGo.SetActive(true);
            UnityObject.Destroy(layerDestroyGo);

            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsTrue(hasRemovedLayers);

            UnityObject.Destroy(layerHold);
        }

        [Test]
        public async Task CompositionLayerManagerTracksLayerState()
        {
            // Create Layer to start manager
            var layerHold = CreateLayerGameObject();
            var layerHoldGo = layerHold.gameObject;
            layerHoldGo.SetActive(true);
            layerHold.Order = 100;
            await new WaitForUpdateCall(this);

            var layer = CreateLayerGameObject();
            layer.Order = 1;
            layer.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);

            Assert.IsTrue(hasCreatedLayers);
            Assert.IsTrue(lastActiveLayers.Count > 1);
            Assert.IsFalse(hasModifiedLayers);
            Assert.IsFalse(hasRemovedLayers);

            var managerLayerId = lastCreatedLayers[0].Id;
            Assert.AreEqual(layer, lastCreatedLayers[0].Layer);
            Assert.AreEqual(lastActiveLayers[1].Layer, lastCreatedLayers[0].Layer);

            layer.gameObject.SetActive(false);
            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsFalse(lastActiveLayers.Count > 2);
            Assert.IsTrue(hasModifiedLayers);
            Assert.IsFalse(hasRemovedLayers);

            layer.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsTrue(lastActiveLayers.Count > 2);
            Assert.IsTrue(hasModifiedLayers);
            Assert.IsFalse(hasRemovedLayers);

            Assert.AreEqual(managerLayerId, lastModifiedLayers[0].Id);
            Assert.AreEqual(layer, lastModifiedLayers[0].Layer);
            Assert.AreEqual(lastActiveLayers[1].Layer, lastModifiedLayers[0].Layer);

            UnityObject.Destroy(layer.gameObject);

            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsFalse(lastActiveLayers.Count > 2);
            Assert.IsFalse(hasModifiedLayers);
            Assert.IsTrue(hasRemovedLayers);
            Assert.AreEqual(managerLayerId, lastRemovedLayers[0]);

            await new WaitForUpdateCall(this);

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsFalse(lastActiveLayers.Count > 2);
            Assert.IsFalse(hasModifiedLayers);
            Assert.IsFalse(hasRemovedLayers);

            UnityObject.Destroy(layerHold.gameObject);
        }

        [Test]
        public async Task CompositionLayerManagerSortsLayersCorrectly()
        {
            var layerOne = CreateLayerGameObject(false);
            layerOne.Order = 0;

            var layerTwo = CreateLayerGameObject(false);
            var layerFive = CreateLayerGameObject(false);
            layerFive.Order = 5;

            var layerSix = CreateLayerGameObject(false);
            layerSix.Order = 5;

            var layerNegOne = CreateLayerGameObject(false);
            layerNegOne.Order = -1;

            var layerNegTwo = CreateLayerGameObject(false);
            layerNegTwo.Order = -1;

            Assert.IsFalse(hasCreatedLayers);
            Assert.IsFalse(hasModifiedLayers);
            Assert.IsFalse(hasRemovedLayers);

            // Checks that it is after default layer and does not collide with default layer order
            layerOne.gameObject.SetActive(true);

            await new WaitForUpdateCall(this);

            Assert.AreEqual(CompositionLayerManager.Instance.OccupiedLayers.Count, lastActiveLayers.Count);
            Assert.AreEqual(lastActiveLayers[1].Layer, layerOne);
            Assert.AreEqual(layerOne.Order, 1);

            // Layer under default is before it in the active layers
            layerNegOne.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);
            Assert.AreEqual(lastActiveLayers[0].Layer, layerNegOne);
            Assert.AreEqual(lastActiveLayers[2].Layer, layerOne);
            Assert.AreEqual(layerNegOne.Order, -1);

            // Layer collision on new underlay layer finds next available underlay index
            layerNegTwo.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);
            Assert.AreEqual(lastActiveLayers[0].Layer, layerNegTwo);
            Assert.AreEqual(lastActiveLayers[1].Layer, layerNegOne);
            Assert.AreEqual(layerNegTwo.Order, -2);

            // New overlay layer is above previous layers
            layerTwo.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);
            var maxIndex = lastActiveLayers.Count - 1;
            Assert.AreEqual(lastActiveLayers[maxIndex].Layer, layerTwo);
            Assert.AreEqual(layerTwo.Order, 2);

            // non contiguous orders are respected
            layerFive.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);
            Assert.AreEqual(layerFive.Order, 5);

            // Layer collisions of non contiguous layers fill to the outside
            layerSix.gameObject.SetActive(true);
            await new WaitForUpdateCall(this);
            maxIndex = lastActiveLayers.Count - 1;
            Assert.AreEqual(lastActiveLayers[maxIndex - 1].Layer, layerFive);
            Assert.AreEqual(lastActiveLayers[maxIndex].Layer, layerSix);
            Assert.AreEqual(layerFive.Order, 5);
            Assert.AreEqual(layerSix.Order, 6);

            // Able to set layers to open order
            layerSix.Order = 4;
            // Able to set layer to newly opened value as soon as other value changes
            layerFive.Order = 6;
            await new WaitForUpdateCall(this);
            Assert.AreEqual(layerFive.Order, 6);
            Assert.AreEqual(layerSix.Order, 4);

            // Able to set layers to order they previously held as soon as that order value is free
            layerFive.Order = 5;
            layerSix.Order = 6;
            await new WaitForUpdateCall(this);
            Assert.AreEqual(layerFive.Order, 5);
            Assert.AreEqual(layerSix.Order, 6);

            // Able to swap layers
            Assert.True(CompositionLayerUtils.TrySwapLayers(layerNegTwo, layerSix));
            await new WaitForUpdateCall(this);
            maxIndex = lastActiveLayers.Count - 1;
            Assert.AreEqual(lastActiveLayers[0].Layer, layerSix);
            Assert.AreEqual(lastActiveLayers[maxIndex].Layer, layerNegTwo);
            Assert.AreEqual(layerSix.Order, -2);
            Assert.AreEqual(layerNegTwo.Order, 6);

            // Swap back
            Assert.True(CompositionLayerUtils.TrySwapLayers(layerNegTwo, layerSix));
            await new WaitForUpdateCall(this);
            Assert.AreEqual(layerSix.Order, 6);
            Assert.AreEqual(layerNegTwo.Order, -2);

            // Able to disable a layer and does not sort to active layers
            layerFive.gameObject.SetActive(false);
            layerNegTwo.enabled = false;
            await new WaitForUpdateCall(this);
            Assert.False(FindLayer(lastActiveLayers, layerFive, out _));
            Assert.False(FindLayer(lastActiveLayers, layerNegTwo, out _));

            // Able to enable a layer and does not sort to active layers
            layerFive.gameObject.SetActive(true);
            layerNegTwo.enabled = true;
            await new WaitForUpdateCall(this);
            Assert.True(FindLayer(lastActiveLayers, layerFive, out _));
            Assert.True(FindLayer(lastActiveLayers, layerNegTwo, out _));

            // remove closest to default layer and next layer fills in sort wise
            UnityObject.Destroy(layerNegOne.gameObject);
            UnityObject.Destroy(layerOne.gameObject);
            await new WaitForUpdateCall(this);
            Assert.True(FindLayer(lastActiveLayers, CompositionLayerManager.Instance.DefaultSceneCompositionLayer, out var pivot));
            Assert.AreEqual(lastActiveLayers[pivot - 1].Layer, layerNegTwo);
            Assert.AreEqual(lastActiveLayers[pivot + 1].Layer, layerTwo);
            // Oder values were not changed due to layers removed.
            Assert.AreEqual(layerNegTwo.Order, -2);
            Assert.AreEqual(layerTwo.Order, 2);

            await new WaitForUpdateCall(this);
            UnityObject.Destroy(layerTwo.gameObject);
            UnityObject.Destroy(layerFive.gameObject);
            UnityObject.Destroy(layerSix.gameObject);
            UnityObject.Destroy(layerNegTwo.gameObject);
        }
    }
}
