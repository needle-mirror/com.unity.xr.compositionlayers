using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.TestTools;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    /// <summary>
    /// Specifies that emulation never destroys a projection rig's cameras, and finds them through the rig's
    /// stored references (UUM-160120).
    /// </summary>
    class ProjectionRigCameraLifetimeTests : CompositionLayerManagerTestBase
    {
        const int k_GameObjectLayer = 5;
        const int k_UpdatesToObserve = 5;

        Camera m_MainCamera;

        protected override void Setup()
        {
            base.Setup();
            ForgetNativeProvider();
            m_MainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            m_MainCamera.tag = "MainCamera";
        }

        protected override void Teardown()
        {
            EmulatedLayerProvider.DisconnectEmulatedLayerProvider();
            UnityObject.DestroyImmediate(m_MainCamera.gameObject);
            base.Teardown();
        }

        [Test]
        public async Task EmulatedRig_ProviderReconnect_KeepsCamerasAndRenders()
        {
            var (layer, leftCamera, rightCamera) = CreateActiveProjectionRig();
            await StartEmulation();
            Assert.IsTrue(leftCamera.targetTexture, "Precondition: the rig is not being emulated.");

            await ReconnectEmulation();

            Assert.IsTrue(leftCamera, "The provider disconnect destroyed the Left Camera.");
            Assert.IsTrue(rightCamera, "The provider disconnect destroyed the Right Camera.");
            Assert.AreSame(layer.transform, leftCamera.transform.parent);
            Assert.AreSame(layer.transform, rightCamera.transform.parent);
            AssertRendersInto(layer, leftCamera, rightCamera);
        }

        [Test]
        public void EmulatedRig_Dispose_ReleasesTexturesAndKeepsSceneObjects()
        {
            var (layer, leftCamera, rightCamera) = CreateActiveProjectionRig();
            layer.gameObject.layer = k_GameObjectLayer;
            var emulatedLayer = CreateEmulatedLayer(layer);
            emulatedLayer.UpdateLayer();
            var leftTexture = leftCamera.targetTexture;
            var rightTexture = rightCamera.targetTexture;
            Assert.IsTrue(leftTexture, "Precondition: the rig did not create its eye textures.");

            emulatedLayer.Dispose();

            Assert.IsTrue(leftCamera, "Dispose destroyed the Left Camera.");
            Assert.IsTrue(rightCamera, "Dispose destroyed the Right Camera.");
            Assert.IsNull(leftCamera.targetTexture);
            Assert.IsNull(rightCamera.targetTexture);
            Assert.IsFalse(leftTexture, "Dispose did not release the left eye texture.");
            Assert.IsFalse(rightTexture, "Dispose did not release the right eye texture.");
            Assert.AreEqual(k_GameObjectLayer, layer.gameObject.layer);
        }

        [Test]
        public async Task EmulatedRig_WithoutCameras_ReportsOnce()
        {
            var layer = CreateActiveLayer<ProjectionLayerRigData>();
            layer.gameObject.AddComponent<TexturesExtension>();
            LogAssert.Expect(LogType.Error, new Regex(
                Regex.Escape(layer.name) + ".*" + Regex.Escape(ProjectionLayerRigData.k_MissingCamerasMessage)));

            await StartEmulation();
            for (var i = 0; i < k_UpdatesToObserve; i++)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        [Test]
        public async Task EmulatedRig_UsesCameraReferencesNotChildOrder()
        {
            var (layer, leftCamera, rightCamera) = CreateActiveProjectionRig();
            ((ProjectionLayerRigData)layer.LayerData).SetCameras(leftCamera, rightCamera);
            var otherCamera = new GameObject("Other Camera").AddComponent<Camera>();
            otherCamera.transform.SetParent(layer.transform, false);
            otherCamera.transform.SetSiblingIndex(0);

            await StartEmulation();

            AssertRendersInto(layer, leftCamera, rightCamera);
            Assert.IsNull(otherCamera.targetTexture, "Emulation rendered into a child that is not a rig camera.");
        }

        [Test]
        public async Task EmulatedRig_CameraRemovedWhileEmulated_ReportsOnceAndReleasesTextures(
            [Values] bool deleteCamera)
        {
            var (layer, leftCamera, rightCamera) = CreateActiveProjectionRig();
            await StartEmulation();
            var leftTexture = leftCamera.targetTexture;
            Assert.IsTrue(leftTexture, "Precondition: the rig is not being emulated.");
            LogAssert.Expect(LogType.Error, new Regex(
                Regex.Escape(layer.name) + ".*" + Regex.Escape(ProjectionLayerRigData.k_MissingCamerasMessage)));

            if (deleteCamera)
                UnityObject.DestroyImmediate(leftCamera.gameObject);
            else
                leftCamera.transform.SetParent(m_MainCamera.transform, false);

            for (var i = 0; i < k_UpdatesToObserve; i++)
            {
                await Awaitable.NextFrameAsync();
            }

            Assert.IsFalse(leftTexture, "The eye textures were kept after a camera left the rig.");
            Assert.IsNull(rightCamera.targetTexture);
            if (!deleteCamera)
                Assert.IsNull(leftCamera.targetTexture, "Emulation still renders into a camera outside the rig.");
        }

        [Test]
        public void EmulatedRig_Dispose_LeavesTargetTexturesItDidNotSet()
        {
            var (layer, leftCamera, rightCamera) = CreateActiveProjectionRig();
            var emulatedLayer = CreateEmulatedLayer(layer);
            emulatedLayer.UpdateLayer();
            var otherTexture = new RenderTexture(4, 4, 0);
            try
            {
                leftCamera.targetTexture = otherTexture;
                layer.GetComponent<TexturesExtension>().LeftTexture = otherTexture;

                emulatedLayer.Dispose();

                Assert.AreSame(otherTexture, leftCamera.targetTexture);
                Assert.AreSame(otherTexture, layer.GetComponent<TexturesExtension>().LeftTexture);
                Assert.IsNull(layer.GetComponent<TexturesExtension>().RightTexture);
            }
            finally
            {
                leftCamera.targetTexture = null;
                UnityObject.DestroyImmediate(otherTexture);
            }
        }

        static async Task StartEmulation()
        {
            EmulatedLayerProvider.ConnectEmulatedLayerProvider();
            for (var i = 0; i < k_UpdatesToObserve; i++)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        static async Task ReconnectEmulation()
        {
            EmulatedLayerProvider.DisconnectEmulatedLayerProvider();
            await StartEmulation();
        }

        static void AssertRendersInto(CompositionLayer layer, Camera leftCamera, Camera rightCamera)
        {
            var texturesExtension = layer.GetComponent<TexturesExtension>();
            Assert.IsTrue(leftCamera.targetTexture, "The Left Camera has no live eye texture.");
            Assert.IsTrue(rightCamera.targetTexture, "The Right Camera has no live eye texture.");
            Assert.AreSame(texturesExtension.LeftTexture, leftCamera.targetTexture);
            Assert.AreSame(texturesExtension.RightTexture, rightCamera.targetTexture);
        }

        static EmulatedCompositionLayer CreateEmulatedLayer(CompositionLayer layer)
        {
            var emulatedLayer = new EmulatedCompositionLayer { CompositionLayer = layer };
            emulatedLayer.ChangeEmulatedLayerDataType(
                EmulatedCompositionLayerUtils.GetEmulatedLayerDataType(layer.LayerData.GetType()));
            return emulatedLayer;
        }

        // Builds a rig the way a scene saved before camera references has it, so the tests also cover adoption.
        (CompositionLayer layer, Camera leftCamera, Camera rightCamera) CreateActiveProjectionRig()
        {
            var layer = CreateLayerGameObject(false);
            var leftCamera = new GameObject("Left Camera").AddComponent<Camera>();
            var rightCamera = new GameObject("Right Camera").AddComponent<Camera>();
            leftCamera.transform.SetParent(layer.transform, false);
            rightCamera.transform.SetParent(layer.transform, false);

            layer.ChangeLayerDataType<ProjectionLayerRigData>();
            layer.gameObject.AddComponent<TexturesExtension>();

            layer.gameObject.SetActive(true);
            m_TrackedLayers.Add(layer);
            return (layer, leftCamera, rightCamera);
        }
    }
}
