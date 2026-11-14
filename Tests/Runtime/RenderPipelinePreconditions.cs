using NUnit.Framework;
using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Tests
{
    static class RenderPipelinePreconditions
    {
        // The emulation shaders only have URP and HDRP subshaders. The CI test projects still run the Built-in
        // Render Pipeline, where the shaders are unsupported, so these tests can only pass in an SRP project.
        // Ignore rather than Assume, because the test runner reports an inconclusive result as a failure.
        public static void IgnoreWithoutScriptableRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Assert.Ignore(
                    "Precondition: a URP or HDRP project. The emulation shaders have no Built-in Render Pipeline " +
                    "subshader.");
            }
        }
    }
}
