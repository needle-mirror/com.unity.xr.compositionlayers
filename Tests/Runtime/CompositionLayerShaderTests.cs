using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Tests
{
    class CompositionLayerShaderTests
    {
        // Always passes in the Editor, where Shader.Find sees every shader in the project. Run it in a player to
        // check that the preloaded shader set included the shader.
        [TestCase(CompositionLayerConstants.UberShaderName)]
        [TestCase(CompositionLayerConstants.ColorScaleBiasShaderName)]
        [TestCase(CompositionLayerConstants.BlitCopyHDRShaderName)]
        public void ShaderResolvesByName(string shaderName)
        {
            Assert.IsNotNull(
                Shader.Find(shaderName),
                $"Shader.Find could not resolve '{shaderName}'. The package shaders reach a player through the " +
                $"CompositionLayersRuntimeSettings asset that setup adds to Preloaded Assets.");
        }

        [TestCase(CompositionLayerConstants.UberShaderName)]
        [TestCase(CompositionLayerConstants.ColorScaleBiasShaderName)]
        [TestCase(CompositionLayerConstants.BlitCopyHDRShaderName)]
        public void ShaderCompilesForThisPlatform(string shaderName)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Shader support cannot be evaluated without a graphics device.");

            RenderPipelinePreconditions.IgnoreWithoutScriptableRenderPipeline();
            var shader = Shader.Find(shaderName);

            Assert.IsNotNull(shader, $"Shader.Find could not resolve '{shaderName}'.");
            Assert.IsTrue(
                shader.isSupported,
                $"'{shaderName}' has no supported subshader on {SystemInfo.graphicsDeviceType}, " +
                $"so anything drawn with it renders as error pink.");
        }

        [Test]
        public void EmulationShader_DrawsPixelsBelowHalfAlpha()
        {
            // Rendering needs a GPU. Without one (batch mode with -nographics) nothing can be drawn, so this would
            // fail without a regression.
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "Precondition: a graphics device. Without one nothing renders.");
            RenderPipelinePreconditions.IgnoreWithoutScriptableRenderPipeline();

            // An unsupported shader draws error pink, whose red channel would pass the check below.
            var shader = Shader.Find(CompositionLayerConstants.UberShaderName);
            Assert.IsTrue(shader.isSupported, "The emulation shader is unsupported, so the draw cannot be checked.");

            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            source.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.3f));
            source.Apply();
            var material = new Material(shader);
            material.EnableKeyword("COMPOSITION_LAYERTYPE_PROJECTION");
            material.EnableKeyword("SOURCE_TEXTURE_ON");
            material.SetTexture("_MainTex", source);
            material.SetTexture(EmulatedLayerData.k_RightEyeTex, source);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            var target = new RenderTexture(4, 4, 0, RenderTextureFormat.ARGB32);
            var mesh = CreateFullScreenQuad();
            var buffer = new CommandBuffer();
            var readback = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var previousTarget = RenderTexture.active;

            try
            {
                buffer.SetRenderTarget(target);
                buffer.ClearRenderTarget(false, true, Color.black);
                buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                buffer.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
                Graphics.ExecuteCommandBuffer(buffer);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);

                Assert.That(readback.GetPixel(2, 2).r, Is.GreaterThan(0.1f), "A pixel at alpha 0.3 was discarded.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                buffer.Release();
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(source);
            }
        }

        static Mesh CreateFullScreenQuad()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(1f, -1f, 0f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }
    }
}
