using System.Threading.Tasks;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Emulation.Implementations;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.Rendering;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests
{
    class ProjectionRigEmulationTests : CompositionLayerManagerTestBase
    {
        const float k_Tolerance = 1e-4f;

        [Test]
        public void ApplyEyeProjections_EachRigCameraTakesItsEyesProjection()
        {
            var leftCamera = new GameObject("Left Camera").AddComponent<Camera>();
            var rightCamera = new GameObject("Right Camera").AddComponent<Camera>();
            var leftProjection = Matrix4x4.Frustum(-0.12f, 0.08f, -0.1f, 0.1f, 0.1f, 100f);
            var rightProjection = Matrix4x4.Frustum(-0.08f, 0.12f, -0.1f, 0.1f, 0.1f, 100f);

            try
            {
                ProjectionRigEmulationLayerData.ApplyEyeProjections(
                    leftCamera, rightCamera, leftProjection, rightProjection);

                AssertMatricesEqual(leftProjection, leftCamera.projectionMatrix, "left");
                AssertMatricesEqual(rightProjection, rightCamera.projectionMatrix, "right");
            }
            finally
            {
                UnityObject.DestroyImmediate(leftCamera.gameObject);
                UnityObject.DestroyImmediate(rightCamera.gameObject);
            }
        }

        [Test]
        public void CopyClipPlanes_RigCamerasCullWithTheMainCamerasClipPlanes()
        {
            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 50f;
            var leftCamera = new GameObject("Left Camera").AddComponent<Camera>();
            var rightCamera = new GameObject("Right Camera").AddComponent<Camera>();
            var eyeProjection = Matrix4x4.Perspective(100f, 0.86f, 0.1f, 50f);

            try
            {
                ProjectionRigEmulationLayerData.CopyClipPlanes(mainCamera, leftCamera, rightCamera);
                ProjectionRigEmulationLayerData.ApplyEyeProjections(
                    leftCamera, rightCamera, eyeProjection, eyeProjection);

                AssertCullingClipPlanes(leftCamera, 0.1f, 50f, "left");
                AssertCullingClipPlanes(rightCamera, 0.1f, 50f, "right");
            }
            finally
            {
                UnityObject.DestroyImmediate(mainCamera.gameObject);
                UnityObject.DestroyImmediate(leftCamera.gameObject);
                UnityObject.DestroyImmediate(rightCamera.gameObject);
            }
        }

        [Test]
        public async Task WithoutXRDisplay_RigCamerasFollowMainCameraAndKeepTheirProjection()
        {
            // Specifies the no-headset path only. With an XR display running (Link, an XR simulator, or a device
            // player), the rig cameras take each eye's projection instead, so this would fail without a regression.
            Assume.That(CompositionLayerUtils.IsDisplaySubsystemActive(), Is.False,
                "Precondition: no XR display. With one running, rig cameras take the eye projections by design.");
            ForgetNativeProvider();

            // Unlike the rig cameras' defaults, so a projection copied onto them is caught.
            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.fieldOfView = 100f;
            mainCamera.nearClipPlane = 0.05f;
            mainCamera.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 20f, 30f));
            try
            {
                EmulatedLayerProvider.ConnectEmulatedLayerProvider();
                var (_, leftCamera, rightCamera) = CreateActiveProjectionRig();
                await Awaitable.NextFrameAsync();

                Assert.AreSame(
                    mainCamera, CompositionLayerManager.MainCameraCache, "Test camera is not the main camera.");
                AssertFollowsWithOwnProjection(mainCamera, leftCamera, "left");
                AssertFollowsWithOwnProjection(mainCamera, rightCamera, "right");
            }
            finally
            {
                EmulatedLayerProvider.DisconnectEmulatedLayerProvider();
                UnityObject.DestroyImmediate(mainCamera.gameObject);
            }
        }

        [Test]
        public void EmulatedRig_BindsEachEyesTexture()
        {
            RenderPipelinePreconditions.IgnoreWithoutScriptableRenderPipeline();
            ForgetNativeProvider();
            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";

            try
            {
                var (layer, _, _) = CreateActiveProjectionRig(addTexturesExtension: true);
                Assert.AreSame(
                    mainCamera, CompositionLayerManager.MainCameraCache, "Test camera is not the main camera.");

                // Creating the emulated layer runs the update that creates the eye textures; the next one binds them.
                var emulatedLayer = CreateEmulatedLayer(layer);
                try
                {
                    emulatedLayer.UpdateLayer();

                    var texturesExtension = layer.GetComponent<TexturesExtension>();
                    var material = emulatedLayer.EmulatedLayerData.EmulationMaterial;
                    Assert.IsNotNull(texturesExtension.RightTexture, "The rig did not create its eye textures.");
                    Assert.AreNotSame(texturesExtension.LeftTexture, texturesExtension.RightTexture);
                    Assert.AreSame(texturesExtension.LeftTexture, material.GetTexture(EmulatedLayerData.k_MainTex));
                    Assert.AreSame(
                        texturesExtension.RightTexture, material.GetTexture(EmulatedLayerData.k_RightEyeTex));
                }
                finally
                {
                    emulatedLayer.Dispose();
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(mainCamera.gameObject);
            }
        }

        [Test]
        public void EmulatedProjectionLayer_RightEyeUsesSameTexture()
        {
            RenderPipelinePreconditions.IgnoreWithoutScriptableRenderPipeline();
            var layer = CreateActiveLayer<ProjectionLayerData>(addSuggestedExtensions: true);
            var texture = new Texture2D(4, 4);
            layer.GetComponent<TexturesExtension>().LeftTexture = texture;
            var emulatedLayer = CreateEmulatedLayer(layer);

            try
            {
                emulatedLayer.UpdateLayer();

                var material = emulatedLayer.EmulatedLayerData.EmulationMaterial;
                Assert.AreSame(texture, material.GetTexture(EmulatedLayerData.k_MainTex));
                Assert.AreSame(texture, material.GetTexture(EmulatedLayerData.k_RightEyeTex));
            }
            finally
            {
                emulatedLayer.Dispose();
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EmulatedRig_BlendsPremultipliedLikeNative()
        {
            var (layer, _, _) = CreateActiveProjectionRig();
            layer.LayerData.BlendType = BlendType.Alpha;
            var emulatedLayer = CreateEmulatedLayer(layer);

            try
            {
                AssertBlend(
                    emulatedLayer.EmulatedLayerData.EmulationMaterial, BlendMode.One, BlendMode.OneMinusSrcAlpha);
            }
            finally
            {
                emulatedLayer.Dispose();
            }
        }

        [Test]
        public void EmulatedQuad_KeepsItsBlendType()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            layer.LayerData.BlendType = BlendType.Alpha;
            var emulatedLayer = CreateEmulatedLayer(layer);

            try
            {
                AssertBlend(
                    emulatedLayer.EmulatedLayerData.EmulationMaterial, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }
            finally
            {
                emulatedLayer.Dispose();
            }
        }

        // Culling takes its side planes from the projection but its near and far planes from the camera.
        static void AssertCullingClipPlanes(Camera camera, float near, float far, string eyeName)
        {
            Assert.IsTrue(camera.TryGetCullingParameters(out var parameters), "No culling parameters.");
            var forward = camera.transform.forward;
            var nearPlanes = 0;
            var farPlanes = 0;
            for (var i = 0; i < parameters.cullingPlaneCount; i++)
            {
                var plane = parameters.GetCullingPlane(i);
                var facing = Vector3.Dot(plane.normal, forward);
                if (facing > 0.99f)
                {
                    nearPlanes++;
                    Assert.AreEqual(
                        near, -plane.distance, k_Tolerance, $"The {eyeName} rig camera culls at the wrong near plane.");
                }
                else if (facing < -0.99f)
                {
                    farPlanes++;
                    Assert.AreEqual(
                        far, plane.distance, k_Tolerance, $"The {eyeName} rig camera culls at the wrong far plane.");
                }
            }

            Assert.AreEqual(1, nearPlanes, $"Expected one near plane for the {eyeName} rig camera.");
            Assert.AreEqual(1, farPlanes, $"Expected one far plane for the {eyeName} rig camera.");
        }

        static void AssertBlend(Material material, BlendMode source, BlendMode destination)
        {
            Assert.AreEqual((int)source, material.GetInt("_SrcBlend"), "Color source blend");
            Assert.AreEqual((int)destination, material.GetInt("_DstBlend"), "Color destination blend");
            Assert.AreEqual((int)BlendMode.One, material.GetInt("_AlphaSrcBlend"), "Alpha source blend");
            Assert.AreEqual(
                (int)BlendMode.OneMinusSrcAlpha, material.GetInt("_AlphaDstBlend"), "Alpha destination blend");
        }

        static EmulatedCompositionLayer CreateEmulatedLayer(CompositionLayer layer)
        {
            var emulatedLayer = new EmulatedCompositionLayer { CompositionLayer = layer };
            emulatedLayer.ChangeEmulatedLayerDataType(
                EmulatedCompositionLayerUtils.GetEmulatedLayerDataType(layer.LayerData.GetType()));
            return emulatedLayer;
        }

        // Rig emulation finds the eye cameras as the layer's first two children, so they exist before it enables.
        (CompositionLayer layer, Camera leftCamera, Camera rightCamera) CreateActiveProjectionRig(
            bool addTexturesExtension = false)
        {
            var layer = CreateLayerGameObject(false);
            var leftCamera = new GameObject("Left Camera").AddComponent<Camera>();
            var rightCamera = new GameObject("Right Camera").AddComponent<Camera>();
            leftCamera.transform.SetParent(layer.transform, false);
            rightCamera.transform.SetParent(layer.transform, false);

            layer.ChangeLayerDataType<ProjectionLayerRigData>();
            if (addTexturesExtension)
                layer.gameObject.AddComponent<TexturesExtension>();

            layer.gameObject.SetActive(true);
            m_TrackedLayers.Add(layer);
            return (layer, leftCamera, rightCamera);
        }

        static void AssertFollowsWithOwnProjection(Camera mainCamera, Camera rigCamera, string eyeName)
        {
            var mainTransform = mainCamera.transform;
            var rigTransform = rigCamera.transform;
            Assert.That(Vector3.Distance(mainTransform.position, rigTransform.position), Is.LessThan(k_Tolerance),
                $"The {eyeName} rig camera is not at the main camera's position.");
            Assert.That(Quaternion.Angle(mainTransform.rotation, rigTransform.rotation), Is.LessThan(k_Tolerance),
                $"The {eyeName} rig camera does not have the main camera's rotation.");

            var ownProjection = Matrix4x4.Perspective(
                rigCamera.fieldOfView, rigCamera.aspect, rigCamera.nearClipPlane, rigCamera.farClipPlane);
            AssertMatricesEqual(ownProjection, rigCamera.projectionMatrix, eyeName);
        }

        static void AssertMatricesEqual(Matrix4x4 expected, Matrix4x4 actual, string eyeName)
        {
            for (var i = 0; i < 16; i++)
            {
                Assert.AreEqual(expected[i], actual[i], k_Tolerance,
                    $"The {eyeName} projection differs at element {i}. Expected {expected}, got {actual}.");
            }
        }
    }
}
